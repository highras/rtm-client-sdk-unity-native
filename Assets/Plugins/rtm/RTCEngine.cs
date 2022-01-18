using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using System;
using AOT;
using UnityEngine.Android;
using UnityEngine.Assertions;

namespace com.fpnn.rtm
{ 
    public static class RTCEngine
    {
        delegate void VoiceCallbackDelegate(IntPtr data, int length);
        [MonoPInvokeCallback(typeof(VoiceCallbackDelegate))]
        private static void VoiceCallback(IntPtr data, int length)
        {
            RTMClient client = null;
            Int64 roomId = 0;
            lock (interLocker)
            {
                client = rtcClient;
                roomId = activeRoomId;
            }
            if (client == null)
            { 
                return;
            }
            if (roomId == 0 || client.IsInRTCRoom(roomId) == false)
            { 
                return;
            }
            byte[] payload = new byte[length];
            Marshal.Copy(data, payload, 0, length);
            //Debug.Log("VoiceCallback payload.length=" + payload.Length);
            bool status = client.Voice(payload);
            //if (status == false)
                //Debug.Log("client.Voice false");
        }
        delegate bool ActiveRoomCallbackDelegate();
        [MonoPInvokeCallback(typeof(ActiveRoomCallbackDelegate))]
        private static bool ActiveRoomCallback()
        { 
            lock (interLocker)
            {
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
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void initRTCEngine(VoiceCallbackDelegate callback, ActiveRoomCallbackDelegate activeRoomCallback, int channelNum);

        [DllImport("__Internal")]
        public static extern void openMicrophone();

        [DllImport("__Internal")]
        public static extern void closeMicrophone();

        [DllImport("__Internal")]
        public static extern void openVoicePlay();

        [DllImport("__Internal")]
        public static extern void closeVoicePlay();

        [DllImport("__Internal")]
        private static extern void receiveVoice(long uid, long seq, byte[] data, int length);
#else
        [DllImport("RTCNative")]
        private static extern void initRTCEngine(VoiceCallbackDelegate callback, ActiveRoomCallbackDelegate activeRoomCallback, int channelNum);

        [DllImport("RTCNative")]
        public static extern void openMicrophone();

        [DllImport("RTCNative")]
        internal static extern void closeMicrophone();

        [DllImport("RTCNative")]
        internal static extern void openVoicePlay();

        [DllImport("RTCNative")]
        internal static extern void closeVoicePlay();

        [DllImport("RTCNative")]
        private static extern void receiveVoice(long uid, long seq, byte[] data, int length);
#endif

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        [DllImport("RTCNative")]
        private static extern void destroy();
#endif

#if UNITY_ANDROID
        [DllImport("RTCNative")]
        //private static extern void initRTCEngine(VoiceCallbackDelegate callback, int osVersion, int channelNum);
        private static extern void initRTCEngine(IntPtr application, VoiceCallbackDelegate callback, int osVersion, int channelNum);

        [DllImport("RTCNative")]
        public static extern void headsetStat();

        [DllImport("RTCNative")]
        public static extern void SetLogger(LoggerCallBack callback);
#endif

        private volatile static bool running = false;
        private static Thread routineThread;
        private static System.Object interLocker;
        private static RTMClient rtcClient;
        private static int seqNum = 0;
        private static Int64 timeOffset = 0;
        private static Queue timeOffsetBuffer;
        private static Int64 activeRoomId = -1;

        public static bool setActiveRoomId(Int64 roomId)
        { 
            lock (interLocker)
            {
                if (rtcClient == null)
                    return false;
                if (rtcClient.IsInRTCRoom(roomId) == false)
                    return false;
                activeRoomId = roomId;
                return true;
            }
        }

        public static Int64 getActiveRoomId()
        { 
            lock (interLocker)
            {
                return activeRoomId;
            }
        }

        public static void exitRTCRoom(RTMClient client, long roomId)
        { 
            
            lock (interLocker)
            {
                if (roomId != activeRoomId)
                    return;
                if (client == null || client != rtcClient)
                    return;
                activeRoomId = -1;
                client.CloseMicroPhone();
                client.CloseVoicePlay();
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


        public static void Init()
        {
#if RTM_DISABLE_RTC
            Assert.IsTrue(false, "RTC is disabled, please remove the RTM_DISABLE_RTC define in \"Scripting Define Symbols\"");
#endif
            if (running)
                return;
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
            var version = new AndroidJavaClass("android.os.Build$VERSION");
            int osVersion = version.GetStatic<int>("SDK_INT");
            AndroidJavaObject currentActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            IntPtr application = currentActivity.Call<AndroidJavaObject>("getApplicationContext").GetRawObject();
            SetLogger(Log);
#endif
            running = true;
            interLocker = new System.Object();
            timeOffsetBuffer = new Queue();

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            initRTCEngine(VoiceCallback, null, 1);
            //Assert.IsTrue(false, "windows is not supported for now");
#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            initRTCEngine(VoiceCallback, ActiveRoomCallback, 1);
#elif UNITY_IOS
            initRTCEngine(VoiceCallback, ActiveRoomCallback, 1);
#elif UNITY_ANDROID
            //initRTCEngine(VoiceCallback, osVersion, 1);
            initRTCEngine(application, VoiceCallback, osVersion, 1);
#endif

            routineThread = new Thread(Routine);
            routineThread.Start();
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
                client.CloseVoicePlay();
                client.CloseMicroPhone();
                rtcClient = null;
            }
        }

        public static void ReceiveVoice(RTMClient client, long uid, long roomId, long seq, long timeStamp, byte[] data)
        {
            //if (client == null || client != rtcClient)
            //return;
            if (StatusMonitor.IsBackground())
                return;
            lock (interLocker)
            { 
                if (roomId != activeRoomId)
                    return;
            }
            Int64 now = ClientEngine.GetCurrentMilliseconds();
            if (now - timeOffset - timeStamp > 1500)
            {
                //Debug.Log("timeout = " + (now - timeOffset - timeStamp));
                return;
            }
            //Debug.Log("ReceiveVoice");
            receiveVoice(uid, seq, data, data.Length);
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
            client.RTCPause();
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
            client.RTCResume();
        }
    }
}

