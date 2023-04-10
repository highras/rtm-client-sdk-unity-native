using System;
using System.Collections.Generic;
using com.fpnn.proto;
using UnityEngine;

namespace com.fpnn.rtm
{
    public partial class RTMClient
    {
        public bool RequestP2PRTC(Action<long, int> callback, RTCP2PType type, long peerUID, int timeout = 0)
        {
            if (RTCEngine.GetActiveRoomId() != -1)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(0, ErrorCode.RTC_EC_IN_ROOM);
                    });
                return false;
            }
            if (!RTCEngine.IsP2PClosed())
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(0, ErrorCode.RTC_EC_P2P_NOT_CLOSED);
                    });
                return false;
            }

            bool status = requestP2PRTC((long callID, int errorCode) => {
                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    RTCEngine.UpdateRequestTime();
                    RTCEngine.SetP2PInfo(callID, peerUID, RTCP2P_STATE.REQUESTING);
                    //RTCEngine.SetP2PRequestClient(this);
                }
                callback(callID, errorCode);
            }, type, peerUID, timeout);

            return status;
        }

        public int RequestP2PRTC(out long callID, RTCP2PType type, long peerUID, int timeout = 0)
        {
            callID = 0;
            if (RTCEngine.GetActiveRoomId() != -1)
                return ErrorCode.RTC_EC_IN_ROOM;

            if (!RTCEngine.IsP2PClosed())
                return ErrorCode.RTC_EC_P2P_NOT_CLOSED;

            int errorCode = requestP2PRTC(out callID, type, peerUID, timeout);
            if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
            {
                RTCEngine.UpdateRequestTime();
                RTCEngine.SetP2PInfo(callID, peerUID, RTCP2P_STATE.REQUESTING);
                //RTCEngine.SetP2PRequestClient(this);
            }
            return errorCode;
        }
        

        private bool requestP2PRTC(Action<long, int> callback, RTCP2PType type, long peerUID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("requestP2PRTC");
            quest.Param("type", (int)type);
            quest.Param("peerUid", peerUID);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {

                long callID = 0;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        callID = answer.Want<long>("callId");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(callID, errorCode);
            }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        private int requestP2PRTC(out long callID, RTCP2PType type, long peerUID, int timeout = 0)
        {
            callID = 0;
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("requestP2PRTC");
            quest.Param("type", (int)type);
            quest.Param("peerUid", peerUID);

            Answer answer = client.SendQuest(quest, timeout);
            if (answer.IsException())
                return answer.ErrorCode();

            try
            {
                callID = answer.Want<long>("callId");

                return fpnn.ErrorCode.FPNN_EC_OK;
            }
            catch (Exception)
            {
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
            }
        }

        public bool CancelP2PRTC(DoneDelegate callback, long callID, int timeout = 0)
        { 
            if (!RTCEngine.IsP2PRequesting())
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(ErrorCode.RTC_EC_P2P_NOT_REQUESTING);
                    });
                return false;
            }

            bool status = cancelP2PRTC((int errorCode) => {
                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    RTCEngine.SetP2PInfo(-1, -1, RTCP2P_STATE.CLOSED);
                }
                callback(errorCode);
            }, callID, timeout);

            return status;
        }

        public int CancelP2PRTC(long callID, int timeout = 0)
        { 
            if (!RTCEngine.IsP2PRequesting())
                return ErrorCode.RTC_EC_P2P_NOT_REQUESTING;

            int errorCode = cancelP2PRTC(callID, timeout);
            if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
            {
                RTCEngine.SetP2PInfo(-1, -1, RTCP2P_STATE.CLOSED);
            }
            return errorCode;
        }

        private bool cancelP2PRTC(DoneDelegate callback, long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("cancelP2PRTC");
            quest.Param("callId", callID);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;            
        }

        private int cancelP2PRTC(long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("cancelP2PRTC");
            quest.Param("callId", callID);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        public bool CloseP2PRTC(DoneDelegate callback, long callID, int timeout = 0)
        { 
            if (!RTCEngine.IsP2PTalking())
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(ErrorCode.RTC_EC_P2P_NOT_TALKING);
                    });
                return false;
            }

            bool status = closeP2PRTC((int errorCode) => {
                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    RTCEngine.CloseP2PRTC();
                }
                callback(errorCode);
            }, callID, timeout);

            return status;
        }

        public int CloseP2PRTC(long callID, int timeout = 0)
        { 
            if (!RTCEngine.IsP2PTalking())
                return ErrorCode.RTC_EC_P2P_NOT_TALKING;

            int errorCode = closeP2PRTC(callID, timeout);
            if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
            {
                RTCEngine.CloseP2PRTC();
            }
            return errorCode;
        }

        private bool closeP2PRTC(DoneDelegate callback, long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("closeP2PRTC");
            quest.Param("callId", callID);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;            
        }

        private int closeP2PRTC(long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("closeP2PRTC");
            quest.Param("callId", callID);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        public bool AcceptP2PRTC(DoneDelegate callback, long peerUid, RTCP2PType type, long callID, int timeout = 0)
        { 
            if (RTCEngine.GetActiveRoomId() != -1)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(ErrorCode.RTC_EC_IN_ROOM);
                    });
                return false;
            }
            if (!RTCEngine.IsP2PClosed())
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(ErrorCode.RTC_EC_P2P_NOT_CLOSED);
                    });
                return false;
            }

            bool status = GetRTCEndpoint((string endpoint, int errorCode) => { 
                if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                {
                    callback(errorCode);
                    return;
                }
                BuildRTCGateClient(endpoint);
                status = acceptP2PRTC((int errorCode2) => {
                    if (errorCode2 == fpnn.ErrorCode.FPNN_EC_OK)
                    {
                        status = setP2PRequest((int errorCode3) =>
                        {
                            if (errorCode3 == fpnn.ErrorCode.FPNN_EC_OK)
                            {
                                RTCEngine.ClearP2PRequestTime();
                                RTCEngine.InitVoice();
                                if (type == RTCP2PType.Video)
                                    RTCEngine.InitVideo(uid);
 
                                RTCEngine.OpenVoicePlay();
                                RTCEngine.OpenMicroPhone();
                                RTCEngine.SetP2PInfo(callID, peerUid, RTCP2P_STATE.TALKING);
                            }
                            else
                            {
                                callback(errorCode3);
                            }
                        }, (int)ProjectId, uid, type, peerUid, callID, timeout);
                        if (!status && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                            ClientEngine.RunTask(() =>
                            {
                                callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                            });
                    }
                    callback(errorCode2);
                }, callID, timeout);
                if (!status && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });
            });
            if (!status && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });
 
            return status;
        }

        public int AcceptP2PRTC(long peerUid, RTCP2PType type, long callID, int timeout = 0)
        {
            if (RTCEngine.GetActiveRoomId() != -1)
                return ErrorCode.RTC_EC_IN_ROOM;

            if (!RTCEngine.IsP2PClosed())
                return ErrorCode.RTC_EC_P2P_NOT_CLOSED;

            int errorCode = GetRTCEndpoint(out string endpoint);
            if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                return errorCode;
            BuildRTCGateClient(endpoint);
            int errorCode2 = acceptP2PRTC(callID, timeout);
            if (errorCode2 != fpnn.ErrorCode.FPNN_EC_OK)
                return errorCode2;
            int errorCode3 = setP2PRequest((int)ProjectId, uid, type, peerUid, callID, timeout);
            if (errorCode3 != fpnn.ErrorCode.FPNN_EC_OK)
                return errorCode3;
            RTCEngine.ClearP2PRequestTime();
            RTCEngine.InitVoice();
            if (type == RTCP2PType.Video)
                RTCEngine.InitVideo(uid);
            RTCEngine.OpenVoicePlay();
            RTCEngine.OpenMicroPhone();
            RTCEngine.SetP2PInfo(callID, peerUid, RTCP2P_STATE.TALKING);
            return errorCode3;
        }

        private bool acceptP2PRTC(DoneDelegate callback, long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("acceptP2PRTC");
            quest.Param("callId", callID);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;            
        }

        private int acceptP2PRTC(long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("acceptP2PRTC");
            quest.Param("callId", callID);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        public bool RefuseP2PRTC(DoneDelegate callback, long callID, int timeout = 0)
        { 
            bool status = refuseP2PRTC((int errorCode) => {
                callback(errorCode);
            }, callID, timeout);

            return status;
        }
        
        public int RefuseP2PRTC(long callID, int timeout = 0)
        { 
            return refuseP2PRTC(callID, timeout); 
        }

        private bool refuseP2PRTC(DoneDelegate callback, long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("refuseP2PRTC");
            quest.Param("callId", callID);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        private int refuseP2PRTC(long callID, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("refuseP2PRTC");
            quest.Param("callId", callID);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        private bool setP2PRequest(DoneDelegate callback, int pid, long uid, RTCP2PType type, long peerUid, long callId, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("setP2PRequest");
            quest.Param("pid", pid);
            quest.Param("uid", uid);
            quest.Param("type", (int)type);
            quest.Param("peerUid", peerUid);
            quest.Param("callId", callId);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        private int setP2PRequest(int pid, long uid, RTCP2PType type, long peerUid, long callId, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("setP2PRequest");
            quest.Param("pid", pid);
            quest.Param("uid", uid);
            quest.Param("type", (int)type);
            quest.Param("peerUid", peerUid);
            quest.Param("callId", callId);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        public bool VoiceP2P(byte[] data, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
                return false;

            Quest quest = new Quest("voiceP2P", true);
            quest.Param("seq", RTCEngine.nextSeqNum());
            quest.Param("toUid", RTCEngine.GetP2PCallUid());
            quest.Param("timestamp", ClientEngine.GetCurrentMilliseconds() - RTCEngine.getTimeOffset());
            quest.Param("data", data);

            return client.SendQuest(quest, (Answer answer, int errorCode) => { }, timeout);
        }

        public bool VideoP2P(long flags, byte[] data, byte[] sps, byte[] pps, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
                return false;

            Quest quest = new Quest("videoP2P", true);
            quest.Param("seq", RTCEngine.nextSeqNumVideo());
            quest.Param("toUid", RTCEngine.GetP2PCallUid());
            quest.Param("flags", flags);
            quest.Param("timestamp", ClientEngine.GetCurrentMilliseconds() - RTCEngine.getTimeOffset());
            quest.Param("rotation", 0);
            quest.Param("version", 0);
            quest.Param("facing", RTCEngine.IsFrontCamera() ? 1 : 0);
            quest.Param("captureLevel", (int)RTCVideoCaptureLevel.Default);
            quest.Param("data", data);
            quest.Param("sps", sps);
            quest.Param("pps", pps);

            return client.SendQuest(quest, (Answer answer, int errorCode) => { }, timeout);
        }

        public void PushP2PRTCEvent(long callId, long peerUid, RTCP2PType type, RTCP2PEvent p2pEvent)
        { 
            //Cancel = 1,     //对端取消p2p请求
            //Close = 2,      //对端挂断
            //Accpet = 3,     //对端已经接受p2p请求
            //Refuse = 4,     //对端拒绝p2p请求
            //NoAnswer = 5,   //对端无人接听
            if (!RTCEngine.IsCurrentCallID(callId))
                return;

            if (p2pEvent == RTCP2PEvent.Cancel)
            {
                RTCEngine.CloseP2PRTC();
            }
            else if (p2pEvent == RTCP2PEvent.Close)
            {
                RTCEngine.CloseP2PRTC();
            }
            else if (p2pEvent == RTCP2PEvent.Accpet)
            {
                GetRTCEndpoint((string endpoint, int errorCode) =>
                { 
                    if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                    {
                        RTCEngine.CloseP2PRTC();
                        return;
                    }
                    BuildRTCGateClient(endpoint);
                    setP2PRequest((int errorCode2) =>
                    {
                        if (errorCode2 == fpnn.ErrorCode.FPNN_EC_OK)
                        {
                            RTCEngine.ClearP2PRequestTime();
                            RTCEngine.InitVoice();
                            if (type == RTCP2PType.Video)
                                RTCEngine.InitVideo(uid);
                            RTCEngine.OpenVoicePlay();
                            RTCEngine.OpenMicroPhone();
                            RTCEngine.SetP2PInfo(callId, peerUid, RTCP2P_STATE.TALKING);
                            //RTCEngine.ClearP2PRequestClient();
                        }
                        else
                        {
                            RTCEngine.CloseP2PRTC();
                        }
                    }, (int)ProjectId, uid, type, peerUid, callId, RTCEngine.rtcP2PTimeout);
                });
                
            }
            else if (p2pEvent == RTCP2PEvent.Refuse)
            {
                RTCEngine.CloseP2PRTC();
            }
            else if (p2pEvent == RTCP2PEvent.NoAnswer)
            {
                RTCEngine.CloseP2PRTC();
            }
        }
    }
}