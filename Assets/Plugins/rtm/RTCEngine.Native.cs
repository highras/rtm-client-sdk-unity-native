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
    public static partial class RTCEngine
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
             {
                 return;
             }
 
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
 #if (UNITY_ANDROID || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_OPENHARMONY)
         [DllImport("RTCNative")]
         private static extern void initRTCEngine(VoiceCallbackDelegate callback, ActiveRoomCallbackDelegate activeRoomCallback, int channelNum);
 
         [DllImport("RTCNative")]
         private static extern void openMicrophone();
 
         [DllImport("RTCNative")]
         private static extern void closeMicrophone();
         
         [DllImport("RTCNative")]
         private static extern void setMicrophoneVolume(int volume);
  
         [DllImport("RTCNative")]
         private static extern void openVoicePlay();
 
         [DllImport("RTCNative")]
         private static extern void closeVoicePlay();
         
         [DllImport("RTCNative")]
         private static extern void setVoicePlayVolume(int volume);

         [DllImport("RTCNative")]
         internal static extern int getUserSoundIntensity(long uid);

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
         private static extern void setMicrophoneVolume(int volume);
  
         [DllImport("__Internal")]
         private static extern void openVoicePlay();
 
         [DllImport("__Internal")]
         private static extern void closeVoicePlay();

         [DllImport("__Internal")]
         private static extern void setVoicePlayVolume(int volume);
  
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
 
#if (!UNITY_OPENHARMONY)
         [DllImport("__Internal")]
         internal static extern void requirePermission(bool microphone, bool camera, PermissionCallbackDelegate callback);
#endif
#else
#endif

#if (UNITY_ANDROID || UNITY_OPENHARMONY)
         [DllImport("RTCNative")]
         private static extern void setUserVolume(long uid, int volume);
         
         [DllImport("RTCNative")]
         private static extern int getRecvStreamVolume(long uid);
         
         [DllImport("RTCNative")]
         private static extern int getSendStreamVolume();
#elif (UNITY_IOS)
        [DllImport("__Internal")]
         private static extern void setUserVolume(long uid, int volume);

         [DllImport("__Internal")]
         private static extern int getRecvStreamVolume(long uid);
                 
         [DllImport("__Internal")]
         private static extern int getSendStreamVolume();
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
         public static extern void headsetStat(int headsetType);
 
         [DllImport("RTCNative")]
         internal static extern void setBackground(bool flag);
 
         [DllImport("RTCNative")]
         internal static extern void stopVoice();
 
         //[DllImport("RTCNative")]
         //public static extern void SetLogger(LoggerCallBack callback);
 
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
             internal PERMISSION_STATUS microphone = PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
             internal PERMISSION_STATUS camera = PERMISSION_STATUS.DENIEDANDDONTASKAGAIN;
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

#if UNITY_OPENHARMONY
    [DllImport("RTCNative")]
    private static extern void initRTCEngine(VoiceCallbackDelegate callback, int channelNum);

    [DllImport("RTCNative")]
    public static extern void headsetStat(int headsetType);

        [DllImport("RTCNative")]
    internal static extern void setBackground(bool flag);
 
 
class OpenHarmonyPermissionCallback
         {
             internal PERMISSION_STATUS microphone = PERMISSION_STATUS.DENIED;
             internal PERMISSION_STATUS camera = PERMISSION_STATUS.DENIED;
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
              
             internal void PermissionCallbacks_PermissionGranted(string permissionName)
             {
                 if (permissionName == "ohos.permission.MICROPHONE")
                 { 
                     microphone = PERMISSION_STATUS.GRANTED;
                     microphoneFinish = true;
                 }
                 else if (permissionName == "ohos.permission.CAMERA")
                 { 
                     camera = PERMISSION_STATUS.GRANTED;
                     cameraFinish = true;
                 }
                 CheckFinish();
             }
 
             internal void PermissionCallbacks_PermissionDenied(string permissionName)
             {
                 if (permissionName == "ohos.permission.MICROPHONE")
                 { 
                     microphone = PERMISSION_STATUS.DENIED;
                     microphoneFinish = true;
                 }
                 else if (permissionName == "ohos.permission.CAMERA")
                 { 
                     camera = PERMISSION_STATUS.DENIED;
                     cameraFinish = true;
                 }
                 CheckFinish();
             }
         }
#endif
    }
}