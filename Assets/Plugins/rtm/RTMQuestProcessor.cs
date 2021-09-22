using System;
using System.Collections.Generic;
using System.Threading;
using com.fpnn.common;
using com.fpnn.proto;
using UnityEngine;

namespace com.fpnn.rtm
{
    public class RTMQuestProcessor
    {
        //----------------[ System Events ]-----------------//
        public virtual void SessionClosed(int ClosedByErrorCode) { }    //-- ErrorCode: com.fpnn.ErrorCode & com.fpnn.rtm.ErrorCode

        //-- Return true for starting relogin, false for stopping relogin.
        public virtual bool ReloginWillStart(int lastErrorCode, int retriedCount) { return true; }
        public virtual void ReloginCompleted(bool successful, bool retryAgain, int errorCode, int retriedCount) { }

        public virtual void Kickout() { }
        public virtual void KickoutRoom(long roomId) { }

        //----------------[ Message Interfaces ]-----------------//
        //-- Messages
        public virtual void PushMessage(RTMMessage message) { }
        public virtual void PushGroupMessage(RTMMessage message) { }
        public virtual void PushRoomMessage(RTMMessage message) { }
        public virtual void PushBroadcastMessage(RTMMessage message) { }

        //-- Chat
        public virtual void PushChat(RTMMessage message) { }
        public virtual void PushGroupChat(RTMMessage message) { }
        public virtual void PushRoomChat(RTMMessage message) { }
        public virtual void PushBroadcastChat(RTMMessage message) { }

        //-- Cmd
        public virtual void PushCmd(RTMMessage message) { }
        public virtual void PushGroupCmd(RTMMessage message) { }
        public virtual void PushRoomCmd(RTMMessage message) { }
        public virtual void PushBroadcastCmd(RTMMessage message) { }

        //-- Files
        public virtual void PushFile(RTMMessage message) { }
        public virtual void PushGroupFile(RTMMessage message) { }
        public virtual void PushRoomFile(RTMMessage message) { }
        public virtual void PushBroadcastFile(RTMMessage message) { }

        //-- RTC
        public virtual void PushEnterRTCRoom(long roomId, long uid, long mtime) { }
        public virtual void PushExitRTCRoom(long roomId, long uid, long mtime) { }
        public virtual void PushRTCRoomClosed(long roomId) { }
        public virtual void PushInviteIntoRTCRoom(long fromUid, long roomId) { }
        public virtual void PushKickOutRTCRoom(long fromUid, long roomId) { }
        public virtual void PushPullIntoRTCRoom(long roomId, string token) { }
        public virtual void PushAdminCommand(RTCAdminCommand command, HashSet<long> uids) { }
        //public virtual void PushVoice(long uid, long roomId, long seq, long time, byte[] data) { }
        //public virtual void PushVideo(long uid, long roomId, long seq, long flags, long timestamp, long rotation, long version, int catureLevel, byte[] data, byte[] sps, byte[] pps) { }
    }

    internal class RTMMasterProcessor: IRTMMasterProcessor
    {
        private RTMQuestProcessor questProcessor;
        private DuplicatedMessageFilter duplicatedFilter;
        private ErrorRecorder errorRecorder;
        private UInt64 connectionId;
        private Int64 lastPingTime;
        private readonly Dictionary<string, QuestProcessDelegate> methodMap;

        public RTMMasterProcessor()
        {
            duplicatedFilter = new DuplicatedMessageFilter();
            lastPingTime = 0;

            methodMap = new Dictionary<string, QuestProcessDelegate> {
                { "ping", Ping },

                { "kickout", Kickout },
                { "kickoutroom", KickoutRoom },

                { "pushmsg", PushMessage },
                { "pushgroupmsg", PushGroupMessage },
                { "pushroommsg", PushRoomMessage },
                { "pushbroadcastmsg", PushBroadcastMessage },

                { "pushEnterRTCRoom", PushEnterRTCRoom },
                { "pushExitRTCRoom", PushExitRTCRoom },
                { "pushRTCRoomClosed", PushRTCRoomClosed },
                { "pushInviteIntoRTCRoom", PushInviteIntoRTCRoom },
                { "pushKickOutRTCRoom", PushKickOutRTCRoom },
                { "pushPullIntoRTCRoom", PushPullIntoRTCRoom },
                { "pushAdminCommand", PushAdminCommand },
          };
        }

        public void SetProcessor(RTMQuestProcessor processor)
        {
            questProcessor = processor;
        }

        public void SetErrorRecorder(ErrorRecorder recorder)
        {
            errorRecorder = recorder;
        }

        public void SetConnectionId(UInt64 connId)
        {
            connectionId = connId;
            Interlocked.Exchange(ref lastPingTime, 0);
        }

        public void BeginCheckPingInterval()
        {
            Int64 now = ClientEngine.GetCurrentSeconds();
            Interlocked.Exchange(ref lastPingTime, now);
        }

        public bool ConnectionIsAlive()
        {
            Int64 lastPingSec = Interlocked.Read(ref lastPingTime);

            if (lastPingSec == 0 || ClientEngine.GetCurrentSeconds() - lastPingSec < RTMConfig.lostConnectionAfterLastPingInSeconds)
                return true;
            else
                return false;
        }

        public QuestProcessDelegate GetQuestProcessDelegate(string method)
        {
            if (methodMap.TryGetValue(method, out QuestProcessDelegate process))
            {
                return process;
            }

            return null;
        }

        public void SessionClosed(int ClosedByErrorCode)
        {
            if (questProcessor != null)
                questProcessor.SessionClosed(ClosedByErrorCode);

            RTMControlCenter.UnregisterSession(connectionId);
        }

        public bool ReloginWillStart(int lastErrorCode, int retriedCount)
        {
            bool startRelogin = true;
            if (questProcessor != null)
                startRelogin = questProcessor.ReloginWillStart(lastErrorCode, retriedCount);

            if (startRelogin)       //-- if startRelogin == false, will call SessionClosed(), the UnregisterSession() will be called in SessionClosed().
                RTMControlCenter.UnregisterSession(connectionId);

            return startRelogin;
        }

        public void ReloginCompleted(bool successful, bool retryAgain, int errorCode, int retriedCount)
        {
            if (questProcessor != null)
                questProcessor.ReloginCompleted(successful, retryAgain, errorCode, retriedCount);
        }

        //----------------------[ RTM Operations ]-------------------//
        public Answer Ping(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            Int64 now = ClientEngine.GetCurrentSeconds();
            Interlocked.Exchange(ref lastPingTime, now);

            return null;
        }

        public Answer Kickout(UInt64 connectionId, string endpoint, Quest quest)
        {
            bool closed = RTMControlCenter.GetClientStatus(connectionId) == RTMClient.ClientStatus.Closed;
            RTMControlCenter.CloseSession(connectionId);

            if (questProcessor != null && closed == false)
                questProcessor.Kickout();

            return null;
        }

        public Answer KickoutRoom(UInt64 connectionId, string endpoint, Quest quest)
        {
            if (questProcessor != null)
            {
                long roomId = quest.Want<Int64>("rid");
                questProcessor.KickoutRoom(roomId);
            }

            return null;
        }

        //----------------------[ RTM Messagess Utilities ]-------------------//
        private TranslatedInfo ProcessChatMessage(Quest quest)
        {
            TranslatedInfo tm = new TranslatedInfo();

            try
            {
                Dictionary<object, object> msg = quest.Want<Dictionary<object, object>>("msg");
                if (msg.TryGetValue("source", out object source))
                {
                    tm.sourceLanguage = (string)source;
                }
                else
                    tm.sourceLanguage = string.Empty;

                if (msg.TryGetValue("target", out object target))
                {
                    tm.targetLanguage = (string)target;
                }
                else
                    tm.targetLanguage = string.Empty;

                if (msg.TryGetValue("sourceText", out object sourceText))
                {
                    tm.sourceText = (string)sourceText;
                }
                else
                    tm.sourceText = string.Empty;

                if (msg.TryGetValue("targetText", out object targetText))
                {
                    tm.targetText = (string)targetText;
                }
                else
                    tm.targetText = string.Empty;

                return tm;
            }
            catch (InvalidCastException e)
            {
                if (errorRecorder != null)
                    errorRecorder.RecordError("ProcessChatMessage failed.", e);

                return null;
            }
        }

        private class MessageInfo
        {
            public bool isBinary;
            public byte[] binaryData;
            public string message;
        }

        private MessageInfo BuildMessageInfo(Quest quest)
        {
            MessageInfo info = new MessageInfo();

            object message = quest.Want("msg");
            info.isBinary = RTMClient.CheckBinaryType(message);
            if (info.isBinary)
                info.binaryData = (byte[])message;
            else
                info.message = (string)message;

            return info;
        }

        private RTMMessage BuildRTMMessage(Quest quest, long from, long to, long mid)
        {
            RTMMessage rtmMessage = new RTMMessage
            {
                fromUid = from,
                toId = to,
                messageId = mid,
                messageType = quest.Want<byte>("mtype"),
                attrs = quest.Want<string>("attrs"),
                modifiedTime = quest.Want<long>("mtime")
            };

            if (rtmMessage.messageType == (byte)MessageType.Chat)
            {
                rtmMessage.translatedInfo = ProcessChatMessage(quest);
                if (rtmMessage.translatedInfo != null)
                {
                    if (rtmMessage.translatedInfo.targetText.Length > 0)
                        rtmMessage.stringMessage = rtmMessage.translatedInfo.targetText;
                    else
                        rtmMessage.stringMessage = rtmMessage.translatedInfo.sourceText;
                }
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                rtmMessage.stringMessage = quest.Want<string>("msg");
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                rtmMessage.stringMessage = quest.Want<string>("msg");
                RTMClient.BuildFileInfo(rtmMessage, errorRecorder);
            }
            else
            {
                MessageInfo messageInfo = BuildMessageInfo(quest);
                if (messageInfo.isBinary)
                {
                    rtmMessage.binaryMessage = messageInfo.binaryData;
                }
                else
                    rtmMessage.stringMessage = messageInfo.message;
            }

            return rtmMessage;
        }

        //----------------------[ RTM Messagess ]-------------------//
        public Answer PushMessage(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long from = quest.Want<long>("from");
            long to = quest.Want<long>("to");
            long mid = quest.Want<long>("mid");

            if (duplicatedFilter.CheckP2PMessage(from, mid) == false)
                return null;

            RTMMessage rtmMessage = BuildRTMMessage(quest, from, to, mid);

            if (rtmMessage.messageType == (byte)MessageType.Chat)
            {
                if (rtmMessage.translatedInfo != null)
                    questProcessor.PushChat(rtmMessage);
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushCmd(rtmMessage);
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushFile(rtmMessage);
            }
            else
            {
                questProcessor.PushMessage(rtmMessage);
            }

            return null;
        }

        public Answer PushGroupMessage(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long groupId = quest.Want<long>("gid");
            long from = quest.Want<long>("from");
            long mid = quest.Want<long>("mid");

            if (duplicatedFilter.CheckGroupMessage(groupId, from, mid) == false)
                return null;

            RTMMessage rtmMessage = BuildRTMMessage(quest, from, groupId, mid);

            if (rtmMessage.messageType == (byte)MessageType.Chat)
            {
                if (rtmMessage.translatedInfo != null)
                    questProcessor.PushGroupChat(rtmMessage);
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushGroupCmd(rtmMessage);
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushGroupFile(rtmMessage);
            }
            else
            {
                questProcessor.PushGroupMessage(rtmMessage);
            }

            return null;
        }

        public Answer PushRoomMessage(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long from = quest.Want<long>("from");
            long roomId = quest.Want<long>("rid");
            long mid = quest.Want<long>("mid");

            if (duplicatedFilter.CheckRoomMessage(roomId, from, mid) == false)
                return null;

            RTMMessage rtmMessage = BuildRTMMessage(quest, from, roomId, mid);

            if (rtmMessage.messageType == (byte)MessageType.Chat)
            {
                if (rtmMessage.translatedInfo != null)
                    questProcessor.PushRoomChat(rtmMessage);
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushRoomCmd(rtmMessage);
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushRoomFile(rtmMessage);
            }
            else
            {
                questProcessor.PushRoomMessage(rtmMessage);
            }

            return null;
        }

        public Answer PushBroadcastMessage(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long from = quest.Want<long>("from");
            long mid = quest.Want<long>("mid");

            if (duplicatedFilter.CheckBroadcastMessage(from, mid) == false)
                return null;

            RTMMessage rtmMessage = BuildRTMMessage(quest, from, 0, mid);

            if (rtmMessage.messageType == (byte)MessageType.Chat)
            {
                if (rtmMessage.translatedInfo != null)
                    questProcessor.PushBroadcastChat(rtmMessage);
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushBroadcastCmd(rtmMessage);
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushBroadcastFile(rtmMessage);
            }
            else
            {
                questProcessor.PushBroadcastMessage(rtmMessage);
            }

            return null;
        }
        public Answer PushEnterRTCRoom(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long roomId = quest.Want<long>("rid");
            long uid = quest.Want<long>("uid");
            long mtime = quest.Want<long>("mtime");

            questProcessor.PushEnterRTCRoom(roomId, uid, mtime);
            return null;
        }

        public Answer PushExitRTCRoom(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long roomId = quest.Want<long>("rid");
            long uid = quest.Want<long>("uid");
            long mtime = quest.Want<long>("mtime");

            questProcessor.PushExitRTCRoom(roomId, uid, mtime);
            return null;
        }

        public Answer PushRTCRoomClosed(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long roomId = quest.Want<long>("rid");

            questProcessor.PushRTCRoomClosed(roomId);
            return null;
        }

        public Answer PushInviteIntoRTCRoom(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long fromUid = quest.Want<long>("fromUid");
            long roomId = quest.Want<long>("rid");

            questProcessor.PushInviteIntoRTCRoom(fromUid, roomId);
            return null;
        }

        public Answer PushKickOutRTCRoom(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long fromUid = quest.Want<long>("fromUid");
            long roomId = quest.Want<long>("rid");

            questProcessor.PushKickOutRTCRoom(fromUid, roomId);
            return null;
        }

        public Answer PushPullIntoRTCRoom(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long roomId = quest.Want<long>("rid");
            string token = quest.Want<string>("token");

            RTMClient client = RTMControlCenter.GetClient(connectionId);
            if (client != null)
            {
                client.enterRTCRoom((bool microphone, HashSet<long> uids, HashSet<long> administrators, long owner, int errorCode) => {
                }, (int)client.ProjectId, client.Uid, roomId, token);
            }

            questProcessor.PushPullIntoRTCRoom(roomId, token);
            return null;
        }

        public Answer PushAdminCommand(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            int command = quest.Want<int>("command");
            HashSet<long> uids = RTMClient.WantLongHashSet(quest, "uids");

            questProcessor.PushAdminCommand((RTCAdminCommand)command, uids);
            return null;
        }
    }

    internal class RTCMasterProcessor: IRTMMasterProcessor
    {
        private RTMQuestProcessor questProcessor;
        private DuplicatedMessageFilter duplicatedFilter;
        private ErrorRecorder errorRecorder;
        private UInt64 connectionId;
        private Int64 lastPingTime;
        private readonly Dictionary<string, QuestProcessDelegate> methodMap;

        public RTCMasterProcessor()
        {
            duplicatedFilter = new DuplicatedMessageFilter();
            lastPingTime = 0;

            methodMap = new Dictionary<string, QuestProcessDelegate> {
                { "ping", Ping },
                { "pushVoice", PushVoice },
                { "pushVideo", PushVideo },
            };
        }

        public void SetProcessor(RTMQuestProcessor processor)
        {
            questProcessor = processor;
        }

        public void SetErrorRecorder(ErrorRecorder recorder)
        {
            errorRecorder = recorder;
        }

        public void SetConnectionId(UInt64 connId)
        {
            connectionId = connId;
            Interlocked.Exchange(ref lastPingTime, 0);
        }

        public void BeginCheckPingInterval()
        {
            Int64 now = ClientEngine.GetCurrentSeconds();
            Interlocked.Exchange(ref lastPingTime, now);
        }

        public bool ConnectionIsAlive()
        {
            Int64 lastPingSec = Interlocked.Read(ref lastPingTime);

            if (lastPingSec == 0 || ClientEngine.GetCurrentSeconds() - lastPingSec < RTMConfig.lostConnectionAfterLastPingInSeconds)
                return true;
            else
                return false;
        }

        public QuestProcessDelegate GetQuestProcessDelegate(string method)
        {
            if (methodMap.TryGetValue(method, out QuestProcessDelegate process))
            {
                return process;
            }

            return null;
        }

        public void SessionClosed(int ClosedByErrorCode)
        {
            if (questProcessor != null)
                questProcessor.SessionClosed(ClosedByErrorCode);

            //RTMControlCenter.UnregisterSession(connectionId);
        }

        public bool ReloginWillStart(int lastErrorCode, int retriedCount)
        {
            bool startRelogin = true;
            if (questProcessor != null)
                startRelogin = questProcessor.ReloginWillStart(lastErrorCode, retriedCount);

            //if (startRelogin)       //-- if startRelogin == false, will call SessionClosed(), the UnregisterSession() will be called in SessionClosed().
                //RTMControlCenter.UnregisterSession(connectionId);

            return startRelogin;
        }

        public void ReloginCompleted(bool successful, bool retryAgain, int errorCode, int retriedCount)
        {
            if (questProcessor != null)
                questProcessor.ReloginCompleted(successful, retryAgain, errorCode, retriedCount);
        }

        //----------------------[ RTM Operations ]-------------------//
        public Answer Ping(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            Int64 now = ClientEngine.GetCurrentSeconds();
            Interlocked.Exchange(ref lastPingTime, now);

            return null;
        }

        public Answer PushVoice(UInt64 connectionId, string endpoint, Quest quest)
        {
            //AdvanceAnswer.SendAnswer(new Answer(quest));
            //if (questProcessor == null)
            //    return null;

            long uid = quest.Want<long>("uid");
            long roomId = quest.Want<long>("rid");
            long seq = quest.Want<long>("seq");
            long timestamp = quest.Want<long>("timestamp");
            byte[] data = quest.Want<byte[]>("data");

            RTMClient client = RTMControlCenter.GetClient(connectionId);

            RTCEngine.ReceiveVoice(client, uid, roomId, seq, timestamp, data);
            //questProcessor.PushVoice(uid, roomId, seq, time, data);
            return null;
        }

        public Answer PushVideo(UInt64 connectionId, string endpoint, Quest quest)
        {
            return null;
            //if (questProcessor == null)
            //    return null;

            //long uid = quest.Want<long>("uid");
            //long roomId = quest.Want<long>("rid");
            //long seq = quest.Want<long>("seq");
            //long flags = quest.Want<long>("flags");
            //long timestamp = quest.Want<long>("timestamp");
            //long rotation = quest.Want<long>("rotation");
            //long version = quest.Want<long>("version");
            //int facing = quest.Want<int>("facing");
            //int captureLevel = quest.Want<int>("captureLevel");
            //byte[] data = quest.Want<byte[]>("data");
            //byte[] sps = quest.Want<byte[]>("sps");
            //byte[] pps = quest.Want<byte[]>("pps");

            //RTCEngine.ReceiveVideo(uid, roomId, seq, flags, timestamp, rotation, version, facing, captureLevel, data, sps, pps);
            ////questProcessor.PushVideo(uid, roomId, seq, flags, timestamp, rotation, version, captureLevel, data, sps, pps);
            //return null;
        }
    }
}
