using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using System;
using AOT;
#if UNITY_ANDROID
using UnityEngine.Android;
#elif UNITY_OPENHARMONY
using UnityEngine.OpenHarmony;
#endif
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
    public static partial class RTCEngine
    {
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
        private static volatile int microphoneVolume = 100;
        private static volatile int voicePlayVolume = 100;

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
            }
            RTCEngine.InitVoice();
            RTCEngine.OpenVoicePlay();
            RTCEngine.CloseMicroPhone();
            return true;
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

            }
            CloseMicroPhone();
            CloseVoicePlay();
            CleanRTC();
            lock (interLocker)
            {
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

                bool hasMicrophone = Permission.HasUserAuthorizedPermission(Permission.Microphone);
                bool hasCamera = Permission.HasUserAuthorizedPermission(Permission.Camera);
                androidPermission.requireMicrophone = requireMicrophone && !hasMicrophone;
                androidPermission.requireCamera = requireCamera && !hasCamera;
                if (requireMicrophone && hasMicrophone)
                    androidPermission.microphone = PERMISSION_STATUS.GRANTED;
                if (requireCamera && hasCamera)
                    androidPermission.camera = PERMISSION_STATUS.GRANTED;

                if (androidPermission.requireMicrophone && androidPermission.requireCamera)
                    Permission.RequestUserPermissions(new string[] { Permission.Microphone, Permission.Camera }, callbacks);
                else if (androidPermission.requireMicrophone)
                    Permission.RequestUserPermissions(new string[] { Permission.Microphone }, callbacks);
                else if (androidPermission.requireCamera)
                    Permission.RequestUserPermissions(new string[] { Permission.Camera }, callbacks);
                else
                {
                    RTMControlCenter.callbackQueue.PostAction(() => {
                        callback?.Invoke(requireMicrophone?PERMISSION_STATUS.GRANTED:PERMISSION_STATUS.DENIEDANDDONTASKAGAIN, requireCamera?PERMISSION_STATUS.GRANTED:PERMISSION_STATUS.DENIEDANDDONTASKAGAIN);
                    });
                }
            }

#elif UNITY_IOS
            internalPermissionCallback = callback;
            requirePermission(requireMicrophone, requireCamera, PermissionCallback);
#elif UNITY_OPENHARMONY
            if (!requireMicrophone && !requireCamera)
            { 
                RTMControlCenter.callbackQueue.PostAction(() => {
                    callback?.Invoke(PERMISSION_STATUS.DENIEDANDDONTASKAGAIN, PERMISSION_STATUS.DENIEDANDDONTASKAGAIN);
                });
            }
            else
            {
                var callbacks = new PermissionCallbacks();
                var openHarmonyPermission = new OpenHarmonyPermissionCallback();
                callbacks.PermissionDenied += openHarmonyPermission.PermissionCallbacks_PermissionDenied;
                callbacks.PermissionGranted += openHarmonyPermission.PermissionCallbacks_PermissionGranted;
                openHarmonyPermission.callback = callback;

                bool hasMicrophone = Permission.HasUserAuthorizedPermission(Permission.Microphone);
                bool hasCamera = Permission.HasUserAuthorizedPermission(Permission.Camera);
                openHarmonyPermission.requireMicrophone = requireMicrophone && !hasMicrophone;
                openHarmonyPermission.requireCamera = requireCamera && !hasCamera;
                if (requireMicrophone && hasMicrophone)
                    openHarmonyPermission.microphone = PERMISSION_STATUS.GRANTED;
                if (requireCamera && hasCamera)
                    openHarmonyPermission.camera = PERMISSION_STATUS.GRANTED;

                if (openHarmonyPermission.requireMicrophone && openHarmonyPermission.requireCamera)
                    Permission.RequestUserPermissions(new string[] { Permission.Microphone, Permission.Camera }, callbacks);
                else if (openHarmonyPermission.requireMicrophone)
                    Permission.RequestUserPermissions(new string[] { Permission.Microphone }, callbacks);
                else if (openHarmonyPermission.requireCamera)
                    Permission.RequestUserPermissions(new string[] { Permission.Camera }, callbacks);
                else
                {
                    RTMControlCenter.callbackQueue.PostAction(() => {
                        callback?.Invoke(requireMicrophone?PERMISSION_STATUS.GRANTED:PERMISSION_STATUS.DENIED, requireCamera?PERMISSION_STATUS.GRANTED:PERMISSION_STATUS.DENIED);
                    });
                }
            }

 
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
            //SetLogger(Log);

            if (requirePermission)
                RequirePermission(true, requireCamera, null);
            initRTCEngine(application, VoiceCallback, 1);
#elif UNITY_OPENHARMONY
            
            if (requirePermission)
                RequirePermission(true, requireCamera, null);
            initRTCEngine(VoiceCallback, 2);
#endif

            routineThread = new Thread(Routine);
            routineThread.Start();

            //videoBufferThread = new Thread(VideoBufferRoutine);
            //videoBufferThread.Start();
        }

        public static void InitVoice()
        {
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
            initVoice();
#endif
        }

        public static void InitVideo(long uid)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
            initVideo(uid, VideoCallback);
#endif
        }

        private static void CleanRTC()
        {
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
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
            CleanRTC();

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
                int count = 0;
                while (count++ < 20 && running) 
                    Thread.Sleep(100);
                if (!running)
                    break;

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

        //private static void VideoBufferRoutine()
        //{ 
        //    while (running)
        //    {
        //        VideoBuffer buffer = null;
        //        lock (interLocker)
        //        { 
        //            foreach (var bufferList in videoBufferList)
        //            {
        //                if (bufferList.Value.Count > videoBufferSize)
        //                {
        //                    buffer = bufferList.Value.Values[0];
        //                    bufferList.Value.RemoveAt(0);
        //                    break;
        //                }
        //            }
        //        }
        //        if (buffer == null)
        //            videoBufferEvent.WaitOne();
        //        else
        //        {
        //            receiveVideo(buffer.uid, buffer.data, buffer.data.Length, buffer.sps, buffer.sps.Length, buffer.pps, buffer.pps.Length, buffer.flags, buffer.timestamp, buffer.seq, buffer.facing);
        //        }
        //    }
        //}

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
        public static void SetMicrophoneVolume(int volume)
        {
            microphoneVolume = volume;
            setMicrophoneVolume(microphoneVolume);
        }

        public static int GetMicrophoneVolume()
        {
            return microphoneVolume;
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

        public static void SetVoicePlayVolume(int volume)
        {
            voicePlayVolume = volume;
            setVoicePlayVolume(voicePlayVolume);
        }

        public static int GetVoicePlayVolume()
        {
            return voicePlayVolume;
        }

        public static void SetUserVolume(long uid, int volume)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#else
            setUserVolume(uid, volume);
#endif
        }
        
        public static int GetRecvStreamVolume(long uid)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            return 0;
#else
            return getRecvStreamVolume(uid);
#endif
        }

        public static int GetSendStreamVolume()
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            return 0;
#else
            return getSendStreamVolume();
#endif
        }
        public static void OpenCamera()
        {
            camera = true;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
            RTCEngine.openCamera();
#endif
        }

        public static void CloseCamera()
        {
            camera = false;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
            RTCEngine.closeCamera();
#endif
        }

        public static void SwitchCamera()
        {
            if (!camera)
                return;
            frontCamera = !frontCamera;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#elif UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
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
#elif UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
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
            if (force)
            {
                InactiveRTCClient(rtcClient);
            }
            lock (interLocker)
            {
                if (force)
                {
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
            }
            
            SetActiveRoomId(-1);
            SetP2PInfo(-1, -1, RTCP2P_STATE.CLOSED);
            CloseVoicePlay();
            CloseMicroPhone();
            CleanRTC();

            lock (interLocker)
            {
                rtcClient = null;
            }
        }

        public static void ReceiveVoice(UInt64 connectionId, long uid, long seq, long timeStamp, byte[] data)
        {
            if (StatusMonitor.IsBackground())
                return;
            if (rtcClient == null)
                return;
            Client client = rtcClient.GetRTCClient();
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
#elif UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
            if (StatusMonitor.IsBackground())
                return;
            if (rtcClient == null)
                return;
            Client client = rtcClient.GetRTCClient();
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
#elif UNITY_IOS || UNITY_ANDROID || UNITY_OPENHARMONY
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
