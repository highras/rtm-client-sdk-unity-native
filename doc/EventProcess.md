# RTM Client Unity SDK API Docs: Event Process

# Index

[TOC]

## Class RTMQuestProcessor

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
        public virtual void PushP2PRTCRequest(long callId, long peerUid, RTCP2PType type) { }
        public virtual void PushP2PRTCEvent(long callId, long peerUid, RTCP2PType type, RTCP2PEvent p2pEvent) { }

    }

### Session Close Event

	void SessionClosed(int ClosedByErrorCode)

Parameters:

+ `int ClosedByErrorCode`

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means closed by user or kickout cmd.

	Others are the reason for failed.

### Server Pushed Events

All methods in IRTMQuestProcessor interface except for SessionClosed() are server pushed events.

#### Kickout

Current client is kicked.

The session is closed by RTM SDK automatically before the method is called.

#### Push Files Methods

The parameter `message` is the URL of file in CDN.

#### ReloginWillStart & ReloginCompleted

Will triggered when connection lost after **first successful login** if user's token is available and user isn't forbidden.

#### RTC

##### User Enter RTC Room

A user entered the RTC room.

##### User Exit RTC Room

A user exited the RTC room.

##### RTC Room Closed

A RTC room is cloesd.

##### Invited Into RTC Room

A user is invited into the RTC room.
##### Kicked out of RTC Room

A user is kicked out of the RTC room.

##### Pulled into RTC Room

A user is pulled into the RTC room.

##### Administrator Command

Notification of the administrator command.

##### P2P RTC Request

Notification of the P2P request.
##### P2P RTC Event

Notification of the P2P request event.