#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using UnityEngine;
using UnityEngine.Assertions;

namespace com.fpnn.rtm
{
    public enum NetworkType
    { 
        NetworkType_Uninited = -2,
        NetworkType_Unknown = -1,
        NetworkType_Unreachable = 0,
        NetworkType_4G = 1,
        NetworkType_Wifi = 2,
    }
    public class StatusMonitor : Singleton<StatusMonitor>
    {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        [DllImport("RTMNative")]
        private static extern void initNetworkStatusChecker(NetworkStatusDelegate callback);

        [DllImport("RTMNative")]
        private static extern void closeNetworkStatusChecker();

#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        [DllImport("RTMNative")]
        private static extern void initNetworkStatusChecker(NetworkStatusDelegate callback);
#elif UNITY_IOS
        [DllImport("__Internal")]
        private static extern void initNetworkStatusChecker(NetworkStatusDelegate callback);
#elif UNITY_ANDROID
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void HeadsetStatusDelegate(int networkStatus);

        [MonoPInvokeCallback(typeof(HeadsetStatusDelegate))]
        public static void HeadsetStatusCallback(int headsetType)
        {
            if (RTCEngine.GetActiveRoomId() == -1 && RTCEngine.GetP2PCallId() == -1)
                return;
            ClientEngine.RunTask(() =>
            {
                RTCEngine.headsetStat(headsetType);
            });
        }

        static AndroidJavaObject AndroidNativeManager= null;
        class NetChangeListener : AndroidJavaProxy
        {
            Action<int> msgCallback;
            public NetChangeListener(Action<int> callback) : base("com.NetForUnity.INetChange") { msgCallback = callback; }
            public void netChangeNotify(int type)
            {
                msgCallback(type);
            }
        }
        class HeadsetListener: AndroidJavaProxy
        {
            Action<int> headsetCallback;
            public HeadsetListener(Action<int> callback) : base("com.NetForUnity.IHeadsetChange") { headsetCallback = callback; }

            public void headsetChange(int type) // //0-无网 1-移动网络 2-wifi
            {
                headsetCallback(type);
            }
        }
        private static void initNetworkStatusChecker(Action<int> netChangeCallback, Action<int> headersetCallback)
        {
            if (AndroidNativeManager == null)
            {
                AndroidJavaClass playerClass = new AndroidJavaClass("com.NetForUnity.ListenUnity");
                AndroidNativeManager = playerClass.CallStatic<AndroidJavaObject>("getInstance");
            }
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var context = jc.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidNativeManager.Call("registerNetChange", context, new NetChangeListener(netChangeCallback));
            AndroidNativeManager.Call("registerHeadsetChange", context, new HeadsetListener(headersetCallback));
        }
#elif UNITY_OPENHARMONY
        private void NetworkChangedCallback(params OpenHarmonyJSObject[] args)
        {
            int status = args[0].Get<int>("status");
            RTMControlCenter.NetworkChanged((NetworkType)status);
        }  

        private void AudioDeviceChangedCallback(params OpenHarmonyJSObject[] args)
        {
            int status = args[0].Get<int>("status");
            UnityEngine.Debug.Log("AudioDeviceChangedCallback " + status);
            if (RTCEngine.GetActiveRoomId() == -1 && RTCEngine.GetP2PCallId() == -1)
                return;
            ClientEngine.RunTask(() =>
            {
                RTCEngine.headsetStat(status == 0 ? 1 : 0);
            });
        }
#else
#endif
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void NetworkStatusDelegate(int networkStatus);

        [MonoPInvokeCallback(typeof(NetworkStatusDelegate))]
        static void NetworkStatusCallback(int networkStatus)
        {
            RTMControlCenter.NetworkChanged((NetworkType)networkStatus);
        }

        static private bool _isPause;
        static private bool _isFocus;
        static private bool _isBackground;

        internal static bool IsBackground() { return _isBackground; }

        void OnEnable()
        {
            _isPause = false;
            _isFocus = true;
            _isBackground = false;
        }

        public void Init() 
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            initNetworkStatusChecker(NetworkStatusCallback);
            //Assert.IsTrue(false, "windows is not supported for now");
#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            initNetworkStatusChecker(NetworkStatusCallback);
#elif UNITY_IOS
            initNetworkStatusChecker(NetworkStatusCallback);
#elif UNITY_ANDROID
            initNetworkStatusChecker(NetworkStatusCallback, HeadsetStatusCallback);
#elif UNITY_OPENHARMONY
            
            OpenHarmonyJSCallback callback = new OpenHarmonyJSCallback(NetworkChangedCallback);
            OpenHarmonyJSClass openHarmonyJSClass = new OpenHarmonyJSClass("NetworkStatusClass");
            openHarmonyJSClass.CallStatic("initNetworkStatus", callback);

            OpenHarmonyJSCallback deviceCallback = new OpenHarmonyJSCallback(AudioDeviceChangedCallback);
            OpenHarmonyJSClass deviceOpenHarmonyJSClass = new OpenHarmonyJSClass("AudioCapturerDeviceStatusClass");
            deviceOpenHarmonyJSClass.CallStatic("initAudioCapturerDeviceStatus", deviceCallback);
#endif
        }
        public void Close() 
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            closeNetworkStatusChecker();
#endif
        }
            

//#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
//        public void Start()
//        {
//            StartCoroutine(PerSecondCoroutine());
//        }
//
//        public void OnDestroy()
//        {
//            StopAllCoroutines();
//        }
//
//        private IEnumerator PerSecondCoroutine()
//        {
//            yield return new WaitForSeconds(1.0f);
//
//            while (true)
//            {
//                CheckNetworkChange();
//
//                yield return new WaitForSeconds(1.0f);
//            }
//        }
//
//        private void CheckNetworkChange()
//        {
//            int networkStatus = (int)Application.internetReachability;
//            RTMControlCenter.NetworkChanged((NetworkType)networkStatus);
//        }
//#endif

        private void CheckInBackground()
        {
            if (_isPause && !_isFocus)
            {
                if (_isBackground == false)
                {
                    _isBackground = true;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)

#elif (UNITY_ANDROID || UNITY_OPENHARMONY)
                    if (RTCEngine.GetActiveRoomId() != -1 || RTCEngine.GetP2PCallId() != -1)
                    {
                        ClientEngine.RunTask(() =>
                        {
                            RTCEngine.setBackground(true);
                            //RTCEngine.Pause();
                        });
                    }
#endif
                }
            }
            else
            {
                if (_isBackground)
                {
                    _isBackground = false;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)
#elif UNITY_ANDROID || UNITY_OPENHARMONY
                    if (RTCEngine.GetActiveRoomId() != -1 || RTCEngine.GetP2PCallId() != -1)
                    {
                        ClientEngine.RunTask(() =>
                        {
                            RTCEngine.setBackground(false);
                            //RTCEngine.Resume();
                        });
                    }
#endif
                }
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            _isPause = pauseStatus;
            CheckInBackground();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            _isFocus = hasFocus;
            CheckInBackground();
        }
    }
}
#endif
