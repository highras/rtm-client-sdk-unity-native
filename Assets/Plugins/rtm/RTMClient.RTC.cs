using System;
using System.Collections.Generic;
using com.fpnn.proto;
using UnityEngine;

namespace com.fpnn.rtm
{
    public partial class RTMClient
    {
        //public long activeRoomId { get; set; }
        private volatile bool microphone = false;
        private volatile bool voicePlay = false;
        private HashSet<long> rtcRoomList = new HashSet<long>();

        public bool IsInRTCRoom(long roomId)
        { 
            lock (rtcInterLocker)
            {
                return rtcRoomList.Contains(roomId);
            }
        }

        public void OpenMicroPhone()
        {
            voicePlay = true;
            microphone = true;
            RTCEngine.openVoicePlay();
            RTCEngine.openMicrophone();
        }
        public void CloseMicroPhone()
        {
            microphone = false;
            RTCEngine.closeMicrophone();
        }
        public void OpenVoicePlay()
        {
            voicePlay = true;
            RTCEngine.openVoicePlay();
        }
        public void CloseVoicePlay() 
        {
            voicePlay = false;
            microphone = false;
            RTCEngine.closeVoicePlay();
            RTCEngine.closeMicrophone();
        }

        public void RTCPause()
        { 
            RTCEngine.closeVoicePlay();
            RTCEngine.closeMicrophone();
        }

        public void RTCResume()
        {
            if (voicePlay)
                RTCEngine.openVoicePlay();
            else
                RTCEngine.closeVoicePlay();
            if (microphone)
                RTCEngine.openMicrophone();
            else
                RTCEngine.closeMicrophone();
        }

        public void CreateRTCRoom(Action<long, int> callback, long roomId, RTCRoomType roomType, int timeout = 0)
        {
            createRTCRoom((long rid, string token, int errorCode) =>
            {
                if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                {
                    callback(0, errorCode);
                    return;
                }

                enterRTCRoom((bool microphone, HashSet<long> uids, HashSet<long> administrators, long owner, int errorCode2) => {
                    if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                    {
                        callback(0, errorCode2);
                        return;
                    }
                    RTCEngine.openVoicePlay();
                    RTCEngine.closeMicrophone();
                    lock (rtcInterLocker)
                    { 
                        rtcRoomList.Add(roomId);
                    }
                    callback(rid, fpnn.ErrorCode.FPNN_EC_OK);
                }, (int)projectId, uid, roomId, token, timeout);
            }, roomId, roomType, timeout);
        }

        public int CreateRTCRoom(long roomId, RTCRoomType roomType, int timeout = 0)
        {
            int errorCode = createRTCRoom(out string token, roomId, roomType, timeout);
            if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                return errorCode;
            int errorCode2 = enterRTCRoom((int)projectId, uid, roomId, token, out _, out _, out _, out _, timeout);
            if (errorCode2 == fpnn.ErrorCode.FPNN_EC_OK)
            {
                RTCEngine.openVoicePlay();
                RTCEngine.closeMicrophone();
                lock (rtcInterLocker)
                { 
                    rtcRoomList.Add(roomId);
                }
            }
            return errorCode2;
        }

        public void EnterRTCRoom(Action<long, RTCRoomType, int> callback, long roomId, int timeout = 0)
        {
            bool status = enterRTCRoom((string token, RTCRoomType roomType, int errorCode) =>
            {
                if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                {
                    callback(0, RTCRoomType.InvalidRoom, errorCode);
                    return;
                }
                status = enterRTCRoom((bool microphone, HashSet<long> uids, HashSet<long> administrators, long owner, int errorCode2) => {
                    if (errorCode2 != fpnn.ErrorCode.FPNN_EC_OK)
                    {
                        callback(0, RTCRoomType.InvalidRoom, errorCode2);
                        return;
                    }
                    RTCEngine.openVoicePlay();
                    RTCEngine.closeMicrophone();
                    lock (rtcInterLocker)
                    {
                        rtcRoomList.Add(roomId);
                    }
                    callback(roomId, roomType, fpnn.ErrorCode.FPNN_EC_OK);
                }, (int)projectId, uid, roomId, token, timeout);
                if (!status && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(0, RTCRoomType.InvalidRoom, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });
            }, roomId, timeout);
            if (!status && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(0, RTCRoomType.InvalidRoom, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });
        }

        public int EnterRTCRoom(out RTCRoomType roomType,long roomId, int timeout = 0)
        {
            int errorCode = enterRTCRoom(out string token, out roomType, roomId, timeout);
            if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                return errorCode;
            int errorCode2 = enterRTCRoom((int)projectId, uid, roomId, token, out _, out _, out _, out _, timeout);
            if (errorCode2 == fpnn.ErrorCode.FPNN_EC_OK)
            { 
                RTCEngine.openVoicePlay();
                RTCEngine.closeMicrophone();
                lock (rtcInterLocker)
                { 
                    rtcRoomList.Add(roomId);
                }
            }
            return errorCode2;
        }

        public void ExitRTCRoom(Action<int> callback, long roomId, int timeout = 0)
        {
            exitRTCRoom((int errorCode) =>
            {
                if (errorCode != fpnn.ErrorCode.FPNN_EC_OK)
                {
                    callback(errorCode);
                    return;
                }
                RTCEngine.exitRTCRoom(this, roomId);
                lock (rtcInterLocker)
                { 
                    rtcRoomList.Remove(roomId);
                }
                callback(errorCode);
            }, roomId, timeout);
        }

        public int ExitRTCRoom(long roomId, int timeout = 0)
        {
            int errorCode = exitRTCRoom(roomId, timeout);
            if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
            {
                RTCEngine.exitRTCRoom(this, roomId);
                lock (rtcInterLocker)
                { 
                    rtcRoomList.Remove(roomId);
                }
            }
            return errorCode;
        }

        //===========================[ Create RTC Room ]=========================//
        private bool createRTCRoom(Action<long, string, int> callback, long roomId, RTCRoomType roomType, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(0, null, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("createRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("type", (Int32)roomType);
            quest.Param("enableRecord", 0);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {
                long rid = 0;
                string token = null;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        rid = answer.Want<long>("rid");
                        token = answer.Want<string>("token");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(rid, token, errorCode);
            }, timeout);


            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(0, null, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        private int createRTCRoom(out string token, long roomId, RTCRoomType roomType, int timeout = 0)
        {
            token = null;
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("createRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("type", (Int32)roomType); ;
            quest.Param("enableRecord", 0);

            Answer answer = client.SendQuest(quest, timeout);
            if (answer.IsException())
                return answer.ErrorCode();

            try
            {
                token = answer.Want<string>("token");

                return fpnn.ErrorCode.FPNN_EC_OK;
            }
            catch (Exception)
            {
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
            }
        }

        //===========================[ Enter RTC Room ]=========================//
        public bool enterRTCRoom(Action<string, RTCRoomType, int> callback, long roomId, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(null, 0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("enterRTCRoom");
            quest.Param("rid", roomId);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {

                int roomType = 0;
                string token = null;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        roomType = answer.Want<int>("type");
                        token = answer.Want<string>("token");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(token, (RTCRoomType)roomType, errorCode);
            }, timeout);


            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(null, 0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int enterRTCRoom(out string token, out RTCRoomType roomType, long roomId, int timeout = 0)
        {
            token = null;
            roomType = RTCRoomType.InvalidRoom;
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("enterRTCRoom");
            quest.Param("rid", roomId);

            Answer answer = client.SendQuest(quest, timeout);
            if (answer.IsException())
                return answer.ErrorCode();

            try
            {
                token = answer.Want<string>("token");
                roomType = (RTCRoomType)answer.Want<int>("type");

                return fpnn.ErrorCode.FPNN_EC_OK;
            }
            catch (Exception)
            {
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
            }
        }

        //===========================[ Invite User Into RTC Room ]=========================//
        public bool InviteUserIntoRTCRoom(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0)
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

            Quest quest = new Quest("inviteUserIntoRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int InviteUserIntoRTCRoom(long roomId, HashSet<long> uids, int timeout = 0)
        { 
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("inviteUserIntoRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        //===========================[ Exit RTC Room ]=========================//
        private bool exitRTCRoom(DoneDelegate callback, long roomId, int timeout = 0)
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

            Quest quest = new Quest("exitRTCRoom");
            quest.Param("rid", roomId);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        private int exitRTCRoom(long roomId, int timeout = 0)
        { 
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("exitRTCRoom");
            quest.Param("rid", roomId);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        //===========================[ Get RTC Room Members ]=========================//
        public bool GetRTCRoomMembers(Action<HashSet<long>, HashSet<long>, long, int> callback, long roomId, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(null, null, 0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("getRTCRoomMembers");
            quest.Param("rid", roomId);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {

                HashSet<long> uids = null;
                HashSet<long> administrators = null;
                long owner = 0;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        uids = WantLongHashSet(answer, "uids");
                        administrators = WantLongHashSet(answer, "administrators");
                        owner = answer.Want<long>("owner");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(uids, administrators, owner, errorCode);
            }, timeout);


            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(null, null, 0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int GetRTCRoomMembers(out HashSet<long> uids, out HashSet<long> administrators, out long owner, long roomId, int timeout = 0)
        {
            uids = null;
            administrators = null;
            owner = 0;
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("getRTCRoomMembers");
            quest.Param("rid", roomId);

            Answer answer = client.SendQuest(quest, timeout);
            if (answer.IsException())
                return answer.ErrorCode();

            try
            {
                uids = WantLongHashSet(answer, "uids");
                administrators = WantLongHashSet(answer, "administrators");
                owner = answer.Want<long>("owner");
                return fpnn.ErrorCode.FPNN_EC_OK;
            }
            catch (Exception)
            {
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
            }
        }
        //===========================[ Get RTC Room Member Count ]=========================//
        public bool GetRTCRoomMemberCount(Action<int, int> callback, long roomId, int timeout = 0)
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

            Quest quest = new Quest("getRTCRoomMemberCount");
            quest.Param("rid", roomId);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {

                int count = 0;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        count = answer.Want<int>("count");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(count, errorCode);
            }, timeout);


            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int GetRTCRoomMemberCount(out int count, long roomId, int timeout = 0)
        {
            count = 0;
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("getRTCRoomMemberCount");
            quest.Param("rid", roomId);

            Answer answer = client.SendQuest(quest, timeout);
            if (answer.IsException())
                return answer.ErrorCode();

            try
            {
                count = answer.Want<int>("count");
                return fpnn.ErrorCode.FPNN_EC_OK;
            }
            catch (Exception)
            {
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
            }
        }
        //===========================[ Block User Voice In RTC Room ]=========================//
        public bool BlockUserVoiceInRTCRoom(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0)
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

            Quest quest = new Quest("blockUserVoiceInRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int BlockUserVoiceInRTCRoom(long roomId, HashSet<long> uids, int timeout = 0)
        { 
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("blockUserVoiceInRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        //===========================[ Unblock User Voice In RTC Room ]=========================//
        public bool UnblockUserVoiceInRTCRoom(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0)
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

            Quest quest = new Quest("unblockUserVoiceInRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int UnblockUserVoiceInRTCRoom(long roomId, HashSet<long> uids, int timeout = 0)
        { 
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("unblockUserVoiceInRTCRoom");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        //===========================[ Subscribe Video ]=========================//
        public bool SubscribeVideo(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0)
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

            Quest quest = new Quest("subscribeVideo");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int SubscribeVideo(long roomId, HashSet<long> uids, int timeout = 0)
        { 
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("subscribeVideo");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        //===========================[ Unsubscribe Video ]=========================//
        public bool UnsubscribeVideo(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0)
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

            Quest quest = new Quest("unsubscribeVideo");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int UnsubscribeVideo(long roomId, HashSet<long> uids, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("unsubscribeVideo");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        //===========================[ Administrator Command ]=========================//
        public bool AdminCommand(DoneDelegate callback, long roomId, HashSet<long> uids, RTCAdminCommand command, int timeout = 0)
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

            Quest quest = new Quest("adminCommand");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);
            quest.Param("command", (int)command);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => { callback(errorCode); }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int AdminCommand(long roomId, HashSet<long> uids, RTCAdminCommand command, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("adminCommand");
            quest.Param("rid", roomId);
            quest.Param("uids", uids);
            quest.Param("command", (int)command);

            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }

        //===========================[ Enter RTC Room ]=========================//
        internal bool enterRTCRoom(Action<bool, HashSet<long>, HashSet<long>, long, int> callback, int pid, long uid, long roomId, string token,int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(false, null, null, 0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("enterRTCRoom");
            quest.Param("pid", pid);
            quest.Param("uid", uid);
            quest.Param("rid", roomId);
            quest.Param("token", token);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {
                bool microphone = false;
                HashSet<long> uids = null;
                HashSet<long> administrators = null;
                long owner = 0;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        microphone = answer.Want<bool>("microphone");
                        uids = WantLongHashSet(answer, "uids");
                        administrators = WantLongHashSet(answer, "administrators");
                        owner = answer.Want<long>("owner");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(microphone, uids, administrators, owner, errorCode);
            }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(false, null, null, 0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        internal int enterRTCRoom(int pid, long uid, long roomId, string token, out bool microphone, out HashSet<long> uids, out HashSet<long> administrators, out long owner, int timeout = 0)
        {
            microphone = false;
            uids = null;
            administrators = null;
            owner = 0;
            Client client = GetRTCClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("enterRTCRoom");
            quest.Param("pid", pid);
            quest.Param("uid", uid);
            quest.Param("rid", roomId);
            quest.Param("token", token);

            Answer answer = client.SendQuest(quest, timeout);
            microphone = answer.Want<bool>("microphone");
            uids = WantLongHashSet(answer, "uids");
            administrators = WantLongHashSet(answer, "administrators");
            owner = answer.Want<long>("owner");

            return answer.ErrorCode();
        }

        //===========================[ Push Voice ]=========================//
        public bool Voice(byte[] data, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
            {
                return false;
            }

            Quest quest = new Quest("voice", true);
            quest.Param("seq", RTCEngine.nextSeqNum());
            quest.Param("rid", RTCEngine.getActiveRoomId());
            quest.Param("timestamp", ClientEngine.GetCurrentMilliseconds() - RTCEngine.getTimeOffset());
            quest.Param("data", data);

            return client.SendQuest(quest, (Answer answer, int errorCode) => {}, timeout);
        }

        //===========================[ Push Video ]=========================//
        public bool Video(long seq, long roomId, long flags, long timestamp, long rotation, long version, int facing, int captureLevel, byte[] data, byte[] sps, byte[] pps, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
                return false;

            Quest quest = new Quest("video", true);
            quest.Param("seq", seq);
            quest.Param("rid", roomId);
            quest.Param("flags", flags);
            quest.Param("timestamp", timestamp);
            quest.Param("rotation", rotation);
            quest.Param("version", version);
            quest.Param("facing", facing);
            quest.Param("captureLevel", captureLevel);
            quest.Param("data", data);
            quest.Param("sps", sps);
            quest.Param("pps", pps);

            return client.SendQuest(quest, (Answer answer, int errorCode) => { }, timeout);
        }

        //===========================[ Adjust Time ]=========================//
        public bool AdjustTime(Action<long, int> callback, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("adjustTime");

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {
                long ts = 0;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        ts = answer.Want<long>("ts");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(ts, errorCode);
            }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int AdjustTime(out long ts, int timeout = 0)
        {
            ts = 0;
            Client client = GetRTCClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("adjustTime");

            Answer answer = client.SendQuest(quest, timeout);
            ts = answer.Want<long>("ts");

            return answer.ErrorCode();
        }
    }
}