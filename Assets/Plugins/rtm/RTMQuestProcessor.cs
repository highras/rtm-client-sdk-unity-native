using System;
using System.Collections.Generic;
using System.Threading;
using com.fpnn.common;
using com.fpnn.proto;
using UnityEngine;

namespace com.fpnn.rtm
{
    public delegate void SessionClosedDelegate(int ClosedByErrorCode);
    public delegate bool ReloginWillStartDelegate(int lastErrorCode, int retriedCount);
    public delegate void ReloginCompletedDelegate(bool successful, bool retryAgain, int errorCode, int retriedCount);
    public delegate void KickOutDelegate();
    public delegate void KickoutRoomDelegate(long roomId);
    public delegate void PushMessageDelegate(RTMMessage message);
    public delegate void PushEnterRTCRoomDelegate(long roomId, long uid, long mtime);
    public delegate void PushExitRTCRoomDelegate(long roomId, long uid, long mtime);
    public delegate void PushRTCRoomClosedDelegate(long roomId);
    public delegate void PushInviteIntoRTCRoomDelegate(long fromUid, long roomId);
    public delegate void PushKickOutRTCRoomDelegate(long fromUid, long roomId);
    public delegate void PushPullIntoRTCRoomDelegate(long fromUid, string token);
    public delegate void PushAdminCommandDelegate(RTCAdminCommand command, HashSet<long> uids);
    public delegate void PushP2PRTCRequestDelegate(long callId, long peerUid, RTCP2PType type);
    public delegate void PushP2PRTCEventDelegate(long callId, long peerUid, RTCP2PType type, RTCP2PEvent p2pEvent);

    public class RTMQuestProcessor
    {
        //----------------[ System Events ]-----------------//
        public virtual void SessionClosed(int ClosedByErrorCode) { }    //-- ErrorCode: com.fpnn.ErrorCode & com.fpnn.rtm.ErrorCode
        public SessionClosedDelegate SessionClosedCallback;

        //-- Return true for starting relogin, false for stopping relogin.
        public virtual bool ReloginWillStart(int lastErrorCode, int retriedCount) { return true; }
        public ReloginWillStartDelegate ReloginWillStartCallback;

        public virtual void ReloginCompleted(bool successful, bool retryAgain, int errorCode, int retriedCount) { }
        public ReloginCompletedDelegate ReloginCompletedCallback;

        public virtual void Kickout() { }
        public KickOutDelegate KickoutCallback;

        public virtual void KickoutRoom(long roomId) { }
        public KickoutRoomDelegate KickoutRoomCallback;

        //----------------[ Message Interfaces ]-----------------//
        //-- Messages
        public virtual void PushMessage(RTMMessage message) { }
        public PushMessageDelegate PushMessageCallback;

        public virtual void PushGroupMessage(RTMMessage message) { }
        public PushMessageDelegate PushGroupMessageCallback;

        public virtual void PushRoomMessage(RTMMessage message) { }
        public PushMessageDelegate PushRoomMessageCallback;

        public virtual void PushBroadcastMessage(RTMMessage message) { }
        public PushMessageDelegate PushBroadcastMessageCallback;

        //-- Chat
        public virtual void PushChat(RTMMessage message) { }
        public PushMessageDelegate PushChatCallback;

        public virtual void PushGroupChat(RTMMessage message) { }
        public PushMessageDelegate PushGroupChatCallback;

        public virtual void PushRoomChat(RTMMessage message) { }
        public PushMessageDelegate PushRoomChatCallback;

        public virtual void PushBroadcastChat(RTMMessage message) { }
        public PushMessageDelegate PushBroadcastChatCallback;

        //-- Cmd
        public virtual void PushCmd(RTMMessage message) { }
        public PushMessageDelegate PushCmdCallback;

        public virtual void PushGroupCmd(RTMMessage message) { }
        public PushMessageDelegate PushGroupCmdCallback;

        public virtual void PushRoomCmd(RTMMessage message) { }
        public PushMessageDelegate PushRoomCmdCallback;

        public virtual void PushBroadcastCmd(RTMMessage message) { }
        public PushMessageDelegate PushBroadcastCmdCallback;

        //-- Files
        public virtual void PushFile(RTMMessage message) { }
        public PushMessageDelegate PushFileCallback;

        public virtual void PushGroupFile(RTMMessage message) { }
        public PushMessageDelegate PushGroupFileCallback;

        public virtual void PushRoomFile(RTMMessage message) { }
        public PushMessageDelegate PushRoomFileCallback;

        public virtual void PushBroadcastFile(RTMMessage message) { }
        public PushMessageDelegate PushBroadcastFileCallback;

        //-- RTC
        public virtual void PushEnterRTCRoom(long roomId, long uid, long mtime) { }
        public PushEnterRTCRoomDelegate PushEnterRTCRoomCallback;

        public virtual void PushExitRTCRoom(long roomId, long uid, long mtime) { }
        public PushExitRTCRoomDelegate PushExitRTCRoomCallback;

        public virtual void PushRTCRoomClosed(long roomId) { }
        public PushRTCRoomClosedDelegate PushRTCRoomClosedCallback;

        public virtual void PushInviteIntoRTCRoom(long fromUid, long roomId) { }
        public PushInviteIntoRTCRoomDelegate PushInviteIntoRTCRoomCallback;

        public virtual void PushKickOutRTCRoom(long fromUid, long roomId) { }
        public PushKickOutRTCRoomDelegate PushKickOutRTCRoomCallback;

        public virtual void PushPullIntoRTCRoom(long roomId, string token) { }
        public PushPullIntoRTCRoomDelegate PushPullIntoRTCRoomCallback;

        public virtual void PushAdminCommand(RTCAdminCommand command, HashSet<long> uids) { }
        public PushAdminCommandDelegate PushAdminCommandCallback;
        
        public virtual void PushP2PRTCRequest(long callId, long peerUid, RTCP2PType type) { }
        public PushP2PRTCRequestDelegate PushP2PRTCRequestCallback;

        public virtual void PushP2PRTCEvent(long callId, long peerUid, RTCP2PType type, RTCP2PEvent p2pEvent) { }
        public PushP2PRTCEventDelegate PushP2PRTCEventCallback;
    }

    public class RTMMasterProcessor: IRTMMasterProcessor
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
                { "pushP2PRTCRequest", PushP2PRTCRequest },
                { "pushP2PRTCEvent", PushP2PRTCEvent },
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
            { 
                questProcessor.SessionClosed(ClosedByErrorCode);
                if (questProcessor.SessionClosedCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.SessionClosedCallback?.Invoke(ClosedByErrorCode);
                    });
                }
            }

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
            { 
                questProcessor.ReloginCompleted(successful, retryAgain, errorCode, retriedCount);
                if (questProcessor.ReloginCompletedCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.ReloginCompletedCallback?.Invoke(successful, retryAgain, errorCode, retriedCount);
                    });
                }
            }
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
            {
                questProcessor.Kickout();
                if (questProcessor.KickoutCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.KickoutCallback?.Invoke();
                    });
                }
            }

            return null;
        }

        public Answer KickoutRoom(UInt64 connectionId, string endpoint, Quest quest)
        {
            if (questProcessor != null)
            {
                long roomId = quest.Want<Int64>("rid");
                questProcessor.KickoutRoom(roomId);
                if (questProcessor.KickoutRoomCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.KickoutRoomCallback.Invoke(roomId);
                    });
                }
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
                {
                    questProcessor.PushChat(rtmMessage);
                    if (questProcessor.PushChatCallback != null)
                    {
                        RTMControlCenter.callbackQueue.PostAction(() =>
                        {
                            questProcessor.PushChatCallback?.Invoke(rtmMessage);
                        });
                    }
                }
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushCmd(rtmMessage);
                if (questProcessor.PushCmdCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushCmdCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushFile(rtmMessage);
                if (questProcessor.PushFileCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushFileCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else
            {
                questProcessor.PushMessage(rtmMessage);
                if (questProcessor.PushMessageCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushMessageCallback?.Invoke(rtmMessage);
                    });
                }
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
                { 
                    questProcessor.PushGroupChat(rtmMessage);
                    if (questProcessor.PushGroupChatCallback != null)
                    {
                        RTMControlCenter.callbackQueue.PostAction(() =>
                        {
                            questProcessor.PushGroupChatCallback?.Invoke(rtmMessage);
                        });
                    }
                }
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushGroupCmd(rtmMessage);
                if (questProcessor.PushGroupCmdCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushGroupCmdCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushGroupFile(rtmMessage);
                if (questProcessor.PushGroupFileCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushGroupFileCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else
            {
                questProcessor.PushGroupMessage(rtmMessage);
                if (questProcessor.PushGroupMessageCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushGroupMessageCallback?.Invoke(rtmMessage);
                    });
                }
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
                { 
                    questProcessor.PushRoomChat(rtmMessage);
                    if (questProcessor.PushRoomChatCallback != null)
                    {
                        RTMControlCenter.callbackQueue.PostAction(() =>
                        {
                            questProcessor.PushRoomChatCallback?.Invoke(rtmMessage);
                        });
                    }
                }
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushRoomCmd(rtmMessage);
                if (questProcessor.PushRoomCmdCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushRoomCmdCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushRoomFile(rtmMessage);
                if (questProcessor.PushRoomFileCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushRoomFileCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else
            {
                questProcessor.PushRoomMessage(rtmMessage);
                if (questProcessor.PushRoomMessageCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushRoomMessageCallback?.Invoke(rtmMessage);
                    });
                }
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
                { 
                    questProcessor.PushBroadcastChat(rtmMessage);
                    if (questProcessor.PushBroadcastChatCallback != null)
                    {
                        RTMControlCenter.callbackQueue.PostAction(() =>
                        {
                            questProcessor.PushBroadcastChatCallback?.Invoke(rtmMessage);
                        });
                    }
                }
            }
            else if (rtmMessage.messageType == (byte)MessageType.Cmd)
            {
                questProcessor.PushBroadcastCmd(rtmMessage);
                if (questProcessor.PushBroadcastCmdCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushBroadcastCmdCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else if (rtmMessage.messageType >= 40 && rtmMessage.messageType <= 50)
            {
                questProcessor.PushBroadcastFile(rtmMessage);
                if (questProcessor.PushBroadcastFileCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushBroadcastFileCallback?.Invoke(rtmMessage);
                    });
                }
            }
            else
            {
                questProcessor.PushBroadcastMessage(rtmMessage);
                if (questProcessor.PushBroadcastMessageCallback != null)
                {
                    RTMControlCenter.callbackQueue.PostAction(() =>
                    {
                        questProcessor.PushBroadcastMessageCallback?.Invoke(rtmMessage);
                    });
                }
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
            if (questProcessor.PushEnterRTCRoomCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushEnterRTCRoomCallback?.Invoke(roomId, uid, mtime);
                });
            }
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
            if (questProcessor.PushExitRTCRoomCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushExitRTCRoomCallback?.Invoke(roomId, uid, mtime);
                });
            }
            return null;
        }

        public Answer PushRTCRoomClosed(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long roomId = quest.Want<long>("rid");

            questProcessor.PushRTCRoomClosed(roomId);
            if (questProcessor.PushRTCRoomClosedCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushRTCRoomClosedCallback?.Invoke(roomId);
                });
            }
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
            if (questProcessor.PushInviteIntoRTCRoomCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushInviteIntoRTCRoomCallback?.Invoke(fromUid, roomId);
                });
            }
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
            if (questProcessor.PushKickOutRTCRoomCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushKickOutRTCRoomCallback?.Invoke(fromUid, roomId);
                });
            }
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
            if (questProcessor.PushPullIntoRTCRoomCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushPullIntoRTCRoomCallback?.Invoke(roomId, token);
                });
            }
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
            if (questProcessor.PushAdminCommandCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushAdminCommandCallback?.Invoke((RTCAdminCommand)command, uids);
                });
            }
            return null;
        }

        public Answer PushP2PRTCRequest(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long callId = quest.Want<long>("callId");
            long peerUid = quest.Want<long>("peerUid");
            int type = quest.Want<int>("type");

            questProcessor.PushP2PRTCRequest(callId, peerUid, (RTCP2PType)type);
            if (questProcessor.PushP2PRTCRequestCallback != null)
            {
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    questProcessor.PushP2PRTCRequestCallback?.Invoke(callId, peerUid, (RTCP2PType)type);
                });
            }
            return null;
        }

        public Answer PushP2PRTCEvent(UInt64 connectionId, string endpoint, Quest quest)
        {
            AdvanceAnswer.SendAnswer(new Answer(quest));

            if (questProcessor == null)
                return null;

            long callId = quest.Want<long>("callId");
            long peerUid = quest.Want<long>("peerUid");
            RTCP2PType type = (RTCP2PType)quest.Want<int>("type");
            RTCP2PEvent p2pEvent = (RTCP2PEvent)quest.Want<int>("event");

            RTMClient client = RTMControlCenter.GetClient(connectionId);
            if (client != null)
                client.PushP2PRTCEvent(callId, peerUid, type, p2pEvent);
            questProcessor.PushP2PRTCEvent(callId, peerUid, type, p2pEvent);
            RTMControlCenter.callbackQueue.PostAction(() => {
                questProcessor.PushP2PRTCEventCallback?.Invoke(callId, peerUid, type, p2pEvent);
            });
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
                { "pushP2PVoice", PushP2PVoice },
                { "pushP2PVideo", PushP2PVideo },
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
            long uid = quest.Want<long>("uid");
            long roomId = quest.Want<long>("rid");
            long seq = quest.Want<long>("seq");
            long timestamp = quest.Want<long>("timestamp");
            byte[] data = quest.Want<byte[]>("data");

            if (roomId != RTCEngine.GetActiveRoomId())
                return null;

            RTCEngine.ReceiveVoice(connectionId, uid, seq, timestamp, data);
            return null;
        }

        public Answer PushVideo(UInt64 connectionId, string endpoint, Quest quest)
        {
            long uid = quest.Want<long>("uid");
            long roomId = quest.Want<long>("rid");
            long seq = quest.Want<long>("seq");
            long flags = quest.Want<long>("flags");
            long timestamp = quest.Want<long>("timestamp");
            long rotation = quest.Want<long>("rotation");
            long version = quest.Want<long>("version");
            int facing = quest.Want<int>("facing");
            int captureLevel = quest.Want<int>("captureLevel");
            byte[] data = quest.Want<byte[]>("data");
            byte[] sps = quest.Want<byte[]>("sps");
            byte[] pps = quest.Want<byte[]>("pps");

            if (roomId != RTCEngine.GetActiveRoomId())
                return null;

            RTCEngine.ReceiveVideo(connectionId, uid, seq, flags, timestamp, rotation, version, facing, captureLevel, data, sps, pps);
            return null;
        }

        public Answer PushP2PVoice(UInt64 connectionId, string endpoint, Quest quest)
        {
            long uid = quest.Want<long>("uid");
            long seq = quest.Want<long>("seq");
            long timestamp = quest.Want<long>("timestamp");
            byte[] data = quest.Want<byte[]>("data");

            if (uid != RTCEngine.GetP2PCallUid())
                return null;

            RTCEngine.ReceiveVoice(connectionId, uid, seq, timestamp, data);
            return null;
        }
        public Answer PushP2PVideo(UInt64 connectionId, string endpoint, Quest quest)
        {
            long uid = quest.Want<long>("uid");
            long seq = quest.Want<long>("seq");
            long flags = quest.Want<long>("flags");
            long timestamp = quest.Want<long>("timestamp");
            long rotation = quest.Want<long>("rotation");
            long version = quest.Want<long>("version");
            int facing = quest.Want<int>("facing");
            int captureLevel = quest.Want<int>("captureLevel");
            byte[] data = quest.Want<byte[]>("data");
            byte[] sps = quest.Want<byte[]>("sps");
            byte[] pps = quest.Want<byte[]>("pps");

            if (uid != RTCEngine.GetP2PCallUid())
                return null;

            RTCEngine.ReceiveVideo(connectionId, uid, seq, flags, timestamp, rotation, version, facing, captureLevel, data, sps, pps);
            return null;
        }
    }
}
