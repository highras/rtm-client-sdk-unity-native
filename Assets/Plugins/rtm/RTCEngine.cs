using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using System;
using AOT;
using UnityEngine.Android;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace com.fpnn.rtm
{
    public enum RTCP2P_STATE
    {
        CLOSED,
        REQUESTING,
        TALKING,
    }

    public enum PERMISSION_STATUS
    { 
        GRANTED,
        DENIED,
        DENIEDANDDONTASKAGAIN,
    }

    class VideoBuffer
    {
        public long uid;
        public long seq;
        public long flags;
        public long timestamp;
        public long rotation;
        public long version;
        public int facing;
        public int captureLevel;
        public byte[] data;
        public byte[] sps;
        public byte[] pps;
    }
    public static class RTCEngine
    {
        delegate void VoiceCallbackDelegate(IntPtr data, int length);
        [MonoPInvokeCallback(typeof(VoiceCallbackDelegate))]
        private static void VoiceCallback(IntPtr data, int length)
        {
            RTMClient client = null;
            lock (interLocker)
            {
                client = rtcClient;
            }
            if (client == null)
                return;

            Int64 roomId = GetActiveRoomId();
            if (roomId != -1 && client.IsInRTCRoom(roomId))
            {
                byte[] payload = new byte[length];
                Marshal.Copy(data, payload, 0, length);
                //Debug.Log("VoiceCallback payload.length=" + payload.Length);
                bool status = client.Voice(payload);
                //if (status == false)
                //Debug.Log("client.Voice false");
                return;
            }
            Int64 callId = GetP2PCallId();
            if (callId != -1)
            {
                byte[] payload = new byte[length];
                Marshal.Copy(data, payload, 0, length);
                bool status = client.VoiceP2P(payload);
                //if (status == false)
                //Debug.Log("client.Voice false");
                return;
            }
        }

        delegate void VideoCallbackDelegate(IntPtr data, int length, IntPtr sps, int spsLength, IntPtr pps, int ppsLength, int flags);
        [MonoPInvokeCallback(typeof(VideoCallbackDelegate))]
        private static void VideoCallback(IntPtr data, int length, IntPtr sps, int spsLength, IntPtr pps, int ppsLength, int flags)
        {
            RTMClient client = null;
            lock (interLocker)
            {
                client = rtcClient;
            }
            if (client == null)
                return;

            Int64 roomId = GetActiveRoomId();
            if (roomId != -1 && client.IsInRTCRoom(roomId))
            {
                byte[] payload = new byte[length];
                Marshal.Copy(data, payload, 0, length);
                byte[] spsPayload = new byte[spsLength];
                Marshal.Copy(sps, spsPayload, 0, spsLength);
                byte[] ppsPayload = new byte[ppsLength];
                Marshal.Copy(pps, ppsPayload, 0, ppsLength);
                bool status = client.Video(flags, payload, spsPayload, ppsPayload);
                //Debug.Log("Video status = " + status);

                return;
            }
            Int64 callId = GetP2PCallId();
            if (callId != -1)
            {
                byte[] payload = new byte[length];
                Marshal.Copy(data, payload, 0, length);
                byte[] spsPayload = new byte[spsLength];
                Marshal.Copy(sps, spsPayload, 0, spsLength);
                byte[] ppsPayload = new byte[ppsLength];
                Marshal.Copy(pps, ppsPayload, 0, ppsLength);
                bool status = client.VideoP2P(flags, payload, spsPayload, ppsPayload);

                return;
            }

        }

        delegate bool ActiveRoomCallbackDelegate();
        [MonoPInvokeCallback(typeof(ActiveRoomCallbackDelegate))]
        private static bool ActiveRoomCallback()
        {
            lock (interLocker)
            {
                if (p2pCallID != -1)
                {
                    if (p2pState == RTCP2P_STATE.TALKING)
                        return true;
                    else
                        return false;
                }
                else
                    return activeRoomId != -1;
            }
        }

        public delegate void LoggerCallBack(IntPtr log, UInt32 len);
        [MonoPInvokeCallback(typeof(LoggerCallBack))]
        public static void Log(IntPtr log, UInt32 len)
        {
            string payload = Marshal.PtrToStringAnsi(log);
            Debug.Log(payload);
        }

        public delegate void PermissionCallbackDelegate(bool microphone, bool camera);
        [MonoPInvokeCallback(typeof(PermissionCallbackDelegate))]
        private static void PermissionCallback(bool microphone, bool camera)
        {
            RTMControlCenter.callbackQueue.PostAction(() => {
                PERMISSION_STATUS microphonePermission = microphone ? PERMISSION_STATUS.GRANTED : PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
                PERMISSION_STATUS cameraPermission = camera ? PERMISSION_STATUS.GRANTED : PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
                internalPermissionCallback?.Invoke(microphonePermission, cameraPermission);
            });
        }
        private static Action<PERMISSION_STATUS, PERMISSION_STATUS> internalPermissionCallback;
#if (UNITY_ANDROID || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        [DllImport("RTCNative")]
        private static extern void initRTCEngine(VoiceCallbackDelegate callback, ActiveRoomCallbackDelegate activeRoomCallback, int channelNum);

        [DllImport("RTCNative")]
        private static extern void openMicrophone();

        [DllImport("RTCNative")]
        private static extern void closeMicrophone();

        [DllImport("RTCNative")]
        private static extern void openVoicePlay();

        [DllImport("RTCNative")]
        private static extern void closeVoicePlay();

        [DllImport("RTCNative")]
        private static extern void receiveVoice(long uid, long seq, byte[] data, int length);

        [DllImport("RTCNative")]
        private static extern void initVoice();

        [DllImport("RTCNative")]
        private static extern void initVideo(long uid, VideoCallbackDelegate callback);

        [DllImport("RTCNative")]
        private static extern void openCamera();

        [DllImport("RTCNative")]
        private static extern void closeCamera();

        [DllImport("RTCNative")]
        private static extern void switchCamera(bool frontCamera);

        [DllImport("RTCNative")]
        private static extern void getVideoFrame(long uid, IntPtr data, ref int size, ref bool facing);

        [DllImport("RTCNative")]
        private static extern void receiveVideo(long uid, byte[] data, int dataLength, byte[] sps, int spsLength, byte[] pps, int ppsLength, long flags, long timestamp, long seq, int facing);

        [DllImport("RTCNative")]
        private static extern void cleanRTC();

        [DllImport("RTCNative")]
        internal static extern void unsubscribeVideo(long uid);
#elif UNITY_IOS
        [DllImport("__Internal")]
        private static extern void initRTCEngine(VoiceCallbackDelegate callback, ActiveRoomCallbackDelegate activeRoomCallback, int channelNum);

        [DllImport("__Internal")]
        private static extern void openMicrophone();

        [DllImport("__Internal")]
        private static extern void closeMicrophone();

        [DllImport("__Internal")]
        private static extern void openVoicePlay();

        [DllImport("__Internal")]
        private static extern void closeVoicePlay();

        [DllImport("__Internal")]
        private static extern void receiveVoice(long uid, long seq, byte[] data, int length);

        [DllImport("__Internal")]
        private static extern void openCamera();

        [DllImport("__Internal")]
        private static extern void closeCamera();

        [DllImport("__Internal")]
        private static extern void switchCamera(bool frontCamera);

        [DllImport("__Internal")]
        private static extern void getVideoFrame(long uid, IntPtr data, ref int size, ref bool facing);

        [DllImport("__Internal")]
        private static extern void receiveVideo(long uid, byte[] data, int dataLength, byte[] sps, int spsLength, byte[] pps, int ppsLength, long flags, long timestamp, long seq, int facing);

        [DllImport("__Internal")]
        private static extern void initVoice();

        [DllImport("__Internal")]
        private static extern void initVideo(long uid, VideoCallbackDelegate callback);

        [DllImport("__Internal")]
        private static extern void cleanRTC();

        [DllImport("__Internal")]
        internal static extern void unsubscribeVideo(long uid);

        [DllImport("__Internal")]
        internal static extern void requirePermission(bool microphone, bool camera, PermissionCallbackDelegate callback);
#else
#endif

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        [DllImport("RTCNative")]
        private static extern void destroy();
#endif

#if UNITY_ANDROID
        [DllImport("RTCNative")]
        private static extern void initRTCEngine(IntPtr application, VoiceCallbackDelegate callback, int channelNum);
        //private static extern void initRTCEngine(IntPtr application, IntPtr focusObject, VoiceCallbackDelegate callback, int channelNum);

        [DllImport("RTCNative")]
        public static extern void headsetStat();

        [DllImport("RTCNative")]
        internal static extern void setBackground(bool flag);

        [DllImport("RTCNative")]
        internal static extern void stopVoice();

        [DllImport("RTCNative")]
        public static extern void SetLogger(LoggerCallBack callback);

        class AudioFocusListener : AndroidJavaProxy
        {
            public AudioFocusListener() : base("android.media.AudioManager$OnAudioFocusChangeListener") { }
            public void onAudioFocusChange(int focus)
            {
                //AudioManager.AUDIOFOCUS_LOSS_TRANSIENT || AudioManager.AUDIOFOCUS_LOSS
                if (focus == -1 || focus == -2)
                {
                    stopVoice();
                }
                //AudioManager.AUDIOFOCUS_GAIN
                else if (focus == 1)
                {
                    if (GetActiveRoomId() != -1 || GetP2PCallId() != -1)
                        initVoice();
                    else
                        stopVoice();
                }
            }
        }

        class AndroidPermissionCallback
        {
            PERMISSION_STATUS microphone = PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
            PERMISSION_STATUS camera = PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
            internal bool requireMicrophone = false;
            internal bool requireCamera = false;
            bool microphoneFinish = false;
            bool cameraFinish = false;
            internal Action<PERMISSION_STATUS, PERMISSION_STATUS> callback;

            void CheckFinish()
            {
                if (requireMicrophone && !microphoneFinish)
                    return;
                if (requireCamera && !cameraFinish)
                    return;
                RTMControlCenter.callbackQueue.PostAction(()=>
                { 
                    callback?.Invoke(microphone, camera);
                });
            }
            internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
            {
                if (permissionName == "android.permission.RECORD_AUDIO")
                { 
                    microphone = PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
                    microphoneFinish = true;
                }
                else if (permissionName == "android.permission.CAMERA")
                { 
                    camera = PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
                    cameraFinish = true;
                }
                CheckFinish();
            }

            internal void PermissionCallbacks_PermissionGranted(string permissionName)
            {
                if (permissionName == "android.permission.RECORD_AUDIO")
                { 
                    microphone = PERMISSION_STATUS.GRANTED;
                    microphoneFinish = true;
                }
                else if (permissionName == "android.permission.CAMERA")
                { 
                    camera = PERMISSION_STATUS.GRANTED;
                    cameraFinish = true;
                }
                CheckFinish();
            }

            internal void PermissionCallbacks_PermissionDenied(string permissionName)
            {
                if (permissionName == "android.permission.RECORD_AUDIO")
                { 
                    microphone = PERMISSION_STATUS.DENIED;
                    microphoneFinish = true;
                }
                else if (permissionName == "android.permission.CAMERA")
                { 
                    camera = PERMISSION_STATUS.DENIED;
                    cameraFinish = true;
                }
                CheckFinish();
            }
        }



#endif

        private volatile static bool running = false;
        private static Thread routineThread;
        private static System.Object interLocker = new System.Object();
        private static RTMClient rtcClient;
        //private static RTMClient p2pRequestingClient;
        private static int seqNum = 0;
        private static int seqNumVideo = 0;
        private static Int64 timeOffset = 0;
        private static Queue timeOffsetBuffer;
        private static Int64 activeRoomId = -1;
        private static Int64 p2pCallID = -1;
        private static Int64 p2pCallUid = -1;
        private static Int64 rtcP2PRequestTime = 0;
        private static RTCP2P_STATE p2pState = RTCP2P_STATE.CLOSED;
        private static volatile bool microphone = false;
        private static volatile bool voicePlay = false;
        private static volatile bool camera = false;
        private static volatile bool frontCamera = true;

        // Video Buffer
        private static readonly int videoBufferSize = 5;
        private static Dictionary<long, SortedList<long, VideoBuffer>> videoBufferList = new Dictionary<long, SortedList<long, VideoBuffer>>();
        private static AutoResetEvent videoBufferEvent = new AutoResetEvent(false);
        private static Thread videoBufferThread;

        public static readonly int rtcP2PTimeout = 30;

        public static void CloseP2PRTC()
        { 
            SetP2PInfo(-1, -1, RTCP2P_STATE.CLOSED);
            //ClearP2PRequestClient();
            CloseMicroPhone();
            CloseVoicePlay();
            CleanRTC();
        }

        public static bool IsP2PClosed()
        {
            lock (interLocker)
            {
                return p2pState == RTCP2P_STATE.CLOSED;
            }
        }

        public static bool IsP2PRequesting()
        { 
            lock (interLocker)
            {
                return p2pState == RTCP2P_STATE.REQUESTING;
            }
        }

        public static bool IsP2PTalking()
        { 
            lock (interLocker)
            {
                return p2pState == RTCP2P_STATE.TALKING;
            }
        }

        public static bool IsCurrentCallID(long callId)
        { 
            lock (interLocker)
            {
                return p2pCallID == callId;
            }
        }

        public static void CheckRTCP2PRequestTime()
        {
            Int64 time = ClientEngine.GetCurrentMilliseconds();
            lock (interLocker)
            {
                if (rtcP2PRequestTime == 0)
                    return;
                if (time <= rtcP2PRequestTime + rtcP2PTimeout * 1000)
                    return;
                rtcP2PRequestTime = 0;
            }
            RTCEngine.CloseP2PRTC();
        }

        public static void UpdateRequestTime()
        { 
            lock (interLocker)
            {
                rtcP2PRequestTime = ClientEngine.GetCurrentMilliseconds();
            }
        }

        //public static void SetP2PRequestClient(RTMClient client)
        //{ 
        //    lock (interLocker)
        //    {
        //        if (client != null)
        //            p2pRequestingClient = client;
        //    }
        //}

        //public static void ClearP2PRequestClient()
        //{ 
        //    lock (interLocker)
        //    {
        //        p2pRequestingClient = null;
        //    }
        //}

        public static void SetP2PCallID(long callID)
        { 
            lock (interLocker)
            {
                p2pCallID = callID;
            }
        }

        public static void SetP2PState(RTCP2P_STATE state)
        { 
            lock (interLocker)
            {
                p2pState = state;
            }
        }

        public static void SetP2PInfo(long callID, long uid, RTCP2P_STATE state)
        { 
            lock (interLocker)
            {
                p2pCallID = callID;
                p2pCallUid = uid;
                p2pState = state;
            }
        }

        public static void ClearP2PRequestTime()
        { 
            lock (interLocker)
            {
                rtcP2PRequestTime = 0;
            }
        }

        public static bool SetActiveRoomId(Int64 roomId)
        { 
            lock (interLocker)
            {
                if (rtcClient == null)
                    return false;
                if (roomId != -1 && rtcClient.IsInRTCRoom(roomId) == false)
                    return false;
                activeRoomId = roomId;

                RTCEngine.InitVoice();
                RTCEngine.OpenVoicePlay();
                RTCEngine.CloseMicroPhone();
                return true;
            }
        }

        public static Int64 GetActiveRoomId()
        { 
            lock (interLocker)
            {
                return activeRoomId;
            }
        }

        public static Int64 GetP2PCallId()
        { 
            lock (interLocker)
            {
                return p2pCallID;
            }
        }

        public static Int64 GetP2PCallUid()
        { 
            lock (interLocker)
            {
                return p2pCallUid;
            }
        }

        public static void ExitRTCRoom(RTMClient client, long roomId)
        { 
            lock (interLocker)
            {
                if (roomId != activeRoomId)
                    return;
                if (client == null || client != rtcClient)
                    return;
                CloseMicroPhone();
                CloseVoicePlay();
                CleanRTC();
                activeRoomId = -1;
            }
        }

        public static void setTimeOffset(Int64 offset)
        {
            lock (interLocker)
            { 
                timeOffset = offset;
            }
        }
 
        public static Int64 getTimeOffset()
        { 
            lock (interLocker)
            {
                return timeOffset;
            }
        }

        public static int nextSeqNum() 
        {
            lock (interLocker)
            {
                seqNum += 1;
                return seqNum;
            }
        }
        public static int nextSeqNumVideo() 
        {
            lock (interLocker)
            {
                seqNumVideo += 1;
                return seqNumVideo;
            }
        }

        public static void adjustTime(Int64 start, Int64 timestamp)
        {
            Int64 end = ClientEngine.GetCurrentMilliseconds();
            Int64 cost = (end - start) / 2;
            lock (interLocker)
            {
                timeOffsetBuffer.Enqueue(end - cost - timestamp);
                if (timeOffsetBuffer.Count > 100)
                    timeOffsetBuffer.Dequeue();
                Int64 total = 0;
                foreach (Int64 offset in timeOffsetBuffer)
                    total += offset;
                timeOffset = total / timeOffsetBuffer.Count;
            }
        }

        public static void RequirePermission(bool requireMicrophone, bool requireCamera, Action<PERMISSION_STATUS, PERMISSION_STATUS> callback)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_ANDROID
            if (!requireMicrophone && !requireCamera)
            { 
                RTMControlCenter.callbackQueue.PostAction(() => {
                    callback?.Invoke(PERMISSION_STATUS.DENIEDANDDONTASKAGAIN, PERMISSION_STATUS.DENIEDANDDONTASKAGAIN);
                });
            }
            else
            {
                var callbacks = new PermissionCallbacks();
                var androidPermission = new AndroidPermissionCallback();
                callbacks.PermissionDenied += androidPermission.PermissionCallbacks_PermissionDenied;
                callbacks.PermissionGranted += androidPermission.PermissionCallbacks_PermissionGranted;
                callbacks.PermissionDeniedAndDontAskAgain += androidPermission.PermissionCallbacks_PermissionDeniedAndDontAskAgain;
                androidPermission.callback = callback;
                androidPermission.requireMicrophone = requireMicrophone;
                androidPermission.requireCamera = requireCamera;

                if (requireMicrophone)
                {
                    if (requireCamera)
                        Permission.RequestUserPermissions(new string[] { Permission.Microphone, Permission.Camera }, callbacks);
                    else
                        Permission.RequestUserPermissions(new string[] { Permission.Microphone }, callbacks);
                }
                else if (requireCamera)
                {
                    Permission.RequestUserPermissions(new string[] { Permission.Camera }, callbacks);
                }
            }

#elif UNITY_IOS
            internalPermissionCallback = callback;
            requirePermission(requireMicrophone, requireCamera, PermissionCallback);
#endif
        }

        public static void Init(bool requirePermission = true, bool requireCamera = false)
        {
#if RTM_DISABLE_RTC
            Assert.IsTrue(false, "RTC is disabled, please remove the RTM_DISABLE_RTC define in \"Scripting Define Symbols\"");
#endif
            if (running)
                return;

            running = true;
            timeOffsetBuffer = new Queue();

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            initRTCEngine(VoiceCallback, null, 1);
            //Assert.IsTrue(false, "windows is not supported for now");
#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            initRTCEngine(VoiceCallback, ActiveRoomCallback, 1);
#elif UNITY_IOS
            if (requirePermission)
                RequirePermission(true, requireCamera, null);
            initRTCEngine(VoiceCallback, ActiveRoomCallback, 1);
#elif UNITY_ANDROID
            var version = new AndroidJavaClass("android.os.Build$VERSION");
            int osVersion = version.GetStatic<int>("SDK_INT");
            AndroidJavaObject currentActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            IntPtr application = currentActivity.Call<AndroidJavaObject>("getApplicationContext").GetRawObject();
            SetLogger(Log);

            if (requirePermission)
                RequirePermission(true, requireCamera, null);
            initRTCEngine(application, VoiceCallback, 1);
#endif

            routineThread = new Thread(Routine);
            routineThread.Start();

            videoBufferThread = new Thread(VideoBufferRoutine);
            videoBufferThread.Start();
        }

        public static void InitVoice()
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            initVoice();
#endif
        }

        public static void InitVideo(long uid)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            initVideo(uid, VideoCallback);
#endif
        }

        private static void CleanRTC()
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            cleanRTC();
#endif
        }

        public static void Stop()
        {
            if (!running)
                return;
            running = false;
            closeMicrophone();
            closeVoicePlay();

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            destroy();
#endif
            routineThread.Join();
            videoBufferEvent.Set();
            videoBufferThread.Join();
            videoBufferEvent.Close();
        }

        private static void Routine()
        {
            while (running)
            {
                Thread.Sleep(2000);

                RTMClient client = null;
                lock (interLocker)
                {
                    client = rtcClient;
                }
                if (client == null)
                    continue;
                Int64 start = ClientEngine.GetCurrentMilliseconds();
                client.AdjustTime((Int64 timestamp, int errorCode) =>
                {
                    if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                        return;
                    adjustTime(start, timestamp);
                });
                CheckRTCP2PRequestTime();
            }
        }

        private static void VideoBufferRoutine()
        { 
            while (running)
            {
                VideoBuffer buffer = null;
                lock (interLocker)
                { 
                    foreach (var bufferList in videoBufferList)
                    {
                        if (bufferList.Value.Count > videoBufferSize)
                        {
                            buffer = bufferList.Value.Values[0];
                            bufferList.Value.RemoveAt(0);
                            break;
                        }
                    }
                }
                if (buffer == null)
                    videoBufferEvent.WaitOne();
                else
                    receiveVideo(buffer.uid, buffer.data, buffer.data.Length, buffer.sps, buffer.sps.Length, buffer.pps, buffer.pps.Length, buffer.flags, buffer.timestamp, buffer.seq, buffer.facing);
            }
        }

        public static void OpenMicroPhone()
        {
            if (RTCEngine.GetActiveRoomId() == -1 && RTCEngine.GetP2PCallId() == -1)
                return;
            microphone = true;
            RTCEngine.openMicrophone();
        }
        public static void CloseMicroPhone()
        {
            if (RTCEngine.GetActiveRoomId() == -1 && RTCEngine.GetP2PCallId() == -1)
                return;
            microphone = false;
            RTCEngine.closeMicrophone();
        }
        public static void OpenVoicePlay()
        {
            if (RTCEngine.GetActiveRoomId() == -1 && RTCEngine.GetP2PCallId() == -1)
                return;
            voicePlay = true;
            RTCEngine.openVoicePlay();
        }
        public static void CloseVoicePlay() 
        {
            if (RTCEngine.GetActiveRoomId() == -1 && RTCEngine.GetP2PCallId() == -1)
                return;
            voicePlay = false;
            RTCEngine.closeVoicePlay();
        }

        public static void OpenCamera()
        {
            camera = true;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            RTCEngine.openCamera();
#endif
        }

        public static void CloseCamera()
        {
            camera = false;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            RTCEngine.closeCamera();
#endif
        }

        public static void SwitchCamera()
        {
            if (!camera)
                return;
            frontCamera = !frontCamera;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            RTCEngine.switchCamera(frontCamera);
#endif
        }

        public static bool IsFrontCamera()
        {
            return frontCamera;
        }

        public static void UnsubscribeVideo(long uid)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            unsubscribeVideo(uid);
#endif
        }

        public static void RTCPause()
        { 
            closeVoicePlay();
            closeMicrophone();
            closeCamera();
        }

        public static void RTCResume()
        {
            if (voicePlay)
                RTCEngine.openVoicePlay();
            else
                RTCEngine.closeVoicePlay();
            if (microphone)
                RTCEngine.openMicrophone();
            else
                RTCEngine.closeMicrophone();

            if (camera)
            {
                if (rtcClient != null)
                    RTCEngine.InitVideo(rtcClient.Uid);
                RTCEngine.openCamera();
                if (frontCamera == false)
                    RTCEngine.switchCamera(frontCamera);
            }
            else
            {
                RTCEngine.closeCamera();
            }
        }

        public static bool ActiveRTCClient(RTMClient client, bool force = true)
        { 
            lock (interLocker)
            {
                if (force)
                {
                    InactiveRTCClient(rtcClient);
                    rtcClient = client;
                    return true;
                }
                if (rtcClient != null)
                {
                    Debug.Log("Only one RTMClient is actived in the same time. If you want to active this client, please close the old one.");
                    return false;
                }
                rtcClient = client;
                return true;
            }
        }

        public static void InactiveRTCClient(RTMClient client)
        { 
            lock (interLocker)
            { 
                if (rtcClient == null)
                    return;
                if (client != rtcClient)
                    return;
                SetActiveRoomId(-1);
                SetP2PInfo(-1, -1, RTCP2P_STATE.CLOSED);
                CloseVoicePlay();
                CloseMicroPhone();
                CleanRTC();
                rtcClient = null;
            }
        }

        public static void ReceiveVoice(UInt64 connectionId, long uid, long seq, long timeStamp, byte[] data)
        {
            if (StatusMonitor.IsBackground())
                return;
            if (rtcClient == null)
                return;
            //UDPClient client = rtcClient.GetRTCClient();
            TCPClient client = rtcClient.GetRTCClient();
            if (client == null)
                return;
            if (client.ConnectionID() != connectionId)
                return;
            Int64 now = ClientEngine.GetCurrentMilliseconds();
            if (now - timeOffset - timeStamp > 3000)
            {
                //Debug.Log("timeout = " + (now - timeOffset - timeStamp));
                return;
            }
            receiveVoice(uid, seq, data, data.Length);
        }


        public static void ReceiveVideo(UInt64 connectionId, long uid, long seq, long flags, long timeStamp, long rotation, long version, int facing, int captureLevel, byte[] data, byte[] sps, byte[] pps)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            if (StatusMonitor.IsBackground())
                return;
            if (rtcClient == null)
                return;
            //UDPClient client = rtcClient.GetRTCClient();
            TCPClient client = rtcClient.GetRTCClient();
            if (client == null)
                return;
            if (client.ConnectionID() != connectionId)
                return;
            VideoBuffer buffer = new VideoBuffer();
            buffer.uid = uid;
            buffer.seq = seq;
            buffer.flags = flags;
            buffer.timestamp = timeStamp;
            buffer.rotation = rotation;
            buffer.version = version;
            buffer.facing = facing;
            buffer.captureLevel = captureLevel;
            buffer.data = data;
            buffer.sps = sps;
            buffer.pps = pps;

            lock (interLocker)
            {
                SortedList<long, VideoBuffer> bufferList = null;
                if (videoBufferList.TryGetValue(uid, out bufferList) == false)
                {
                    bufferList = new SortedList<long, VideoBuffer>();
                    videoBufferList.Add(uid, bufferList);
                }
                bufferList.Add(seq, buffer);
            }
            videoBufferEvent.Set();
#endif
        }

        public static void GetVideoFrame(long uid, IntPtr data, ref int size, ref bool facing)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID
            getVideoFrame(uid, data, ref size, ref facing);
#endif
        }

        public static void Pause()
        {
            if (interLocker == null)
                return;
            RTMClient client = null;
            lock (interLocker)
            {
                client = rtcClient;
            }
            if (client == null)
                return;
            RTCEngine.RTCPause();
        }

        public static void Resume()
        {
            if (interLocker == null)
                return;
            RTMClient client = null;
            lock (interLocker)
            {
                client = rtcClient;
            }
            if (client == null)
                return;
            RTCEngine.RTCResume();
        }
    }
}

