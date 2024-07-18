# RTM Client Unity SDK RTC API Docs

# Index

[TOC]


### RTC Init (REQUIRED)
	using com.fpnn.rtm;
	RTCEngine.Init();

### Create RTC Room

	//-- Async Method
	public void CreateRTCRoom(Action<long, int> callback, long roomId, RTCRoomType roomType, int timeout = 0);
	
	//-- Sync Method
	public int CreateRTCRoom(long roomId, RTCRoomType roomType, int timeout = 0);

Create RTC room.

Parameters:

+ `Action<long, int> callback`

	Callabck for async method.  
	1. `long` is the created RTC room ID;  
	2. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room ID.

+ `RTCRoomType roomType`

	RTC room type. Please refer [RTCRoomType](Structures.md#RTCRoomType)

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.


### Enter RTC Room

	//-- Async Method
	public void EnterRTCRoom(Action<long, RTCRoomType, int> callback, long roomId, int timeout = 0);
	
	//-- Sync Method
	public int EnterRTCRoom(out RTCRoomType roomType,long roomId, int timeout = 0);

Enter RTC room.

Parameters:

+ `Action<long, RTCRoomType, int> callback`

	Callabck for async method.  
	1. `long` is the entered RTC room ID; 
    2. `RTCRoomType` is the type of the entered RTC room.
	3. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room ID.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.


### Exit RTC Room

	//-- Async Method
	public void ExitRTCRoom(Action<int> callback, long roomId, int timeout = 0);
	
	//-- Sync Method
	public int ExitRTCRoom(long roomId, int timeout = 0);

Exit RTC room.

Parameters:

+ `Action<int> callback`

	Callabck for async method.  
	1. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room ID.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.


### Invite User Into RTC Room

	//-- Async Method
	public bool InviteUserIntoRTCRoom(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0);
	
	//-- Sync Method
	public int InviteUserIntoRTCRoom(long roomId, HashSet<long> uids, int timeout = 0);

Invite user into RTC room.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long roomId`

	RTC room ID.

+ `HashSet<long> uids`

	ID of the users invited into the RTC room.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.


### Get RTC Room Members

	//-- Async Method
	public bool GetRTCRoomMembers(Action<HashSet<long>, HashSet<long>, long, int> callback, long roomId, int timeout = 0);
	
	//-- Sync Method
	public int GetRTCRoomMembers(out HashSet<long> uids, out HashSet<long> administrators, out long owner, long roomId, int timeout = 0);

Get RTC room members.

Parameters:

+ `Action<HashSet<long>, HashSet<long>, long, int> callback`

	Callabck for async method.  
    1. `HashSet<long>` is the uids of the RTC room member.
    2. `HashSet<long>` is the uids of the RTC room administrator.
    2. `HashSet<long>` is the uid of the RTC room owner.
	4. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room ID.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.


### Get RTC Room Member Count

	//-- Async Method
	public bool GetRTCRoomMemberCount(Action<int, int> callback, long roomId, int timeout = 0);
	
	//-- Sync Method
    public int GetRTCRoomMemberCount(out int count, long roomId, int timeout = 0);

Get RTC room member count.

Parameters:

+ `Action<int, int> callback`

	Callabck for async method.  
    1. `int` is the count of the RTC room member.
	2. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room id.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.


### Block User Voice In RTC Room

	//-- Async Method
	public bool BlockUserVoiceInRTCRoom(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0);

	//-- Sync Method
	public int BlockUserVoiceInRTCRoom(long roomId, HashSet<long> uids, int timeout = 0);

Block user voice in RTC room.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long roomId`

	RTC room ID.

+ `HashSet<long> uids`

    Uids of the user to block.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.


### Unblock User Voice In RTC Room


	//-- Async Method
	public bool UnblockUserVoiceInRTCRoom(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0);
	
	//-- Sync Method
    public int UnblockUserVoiceInRTCRoom(long roomId, HashSet<long> uids, int timeout = 0);

Unblock user voice in RTC room.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long roomId`

	RTC room ID.

+ `HashSet<long> uids`

    Uids of the user to block.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.


### Administrator Command

	//-- Async Method
    public bool AdminCommand(DoneDelegate callback, long roomId, HashSet<long> uids, RTCAdminCommand command, int timeout = 0);
	
	//-- Sync Method
	public int AdminCommand(long roomId, HashSet<long> uids, RTCAdminCommand command, int timeout = 0);

Administrator command operation.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long roomId`

	RTC room ID.

+ `HashSet<long> uids`

	Uids of the operation target.

+ `RTCAdminCommand command`

	Type of adminstrator command. Please refer [AdministratorCommand](Structures.md#AdministratorCommand)

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.

### Subscribe User Video Stream

	//-- Async Method
    public bool SubscribeVideo(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0);
	
	//-- Sync Method
	public int SubscribeVideo(long roomId, HashSet<long> uids, int timeout = 0);

Subscribe user video stream.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long roomId`

	RTC room ID.

+ `HashSet<long> uids`

	Uids of the subscribe target.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.

### Unsubscribe User Video Stream

	//-- Async Method
    public bool UnsubscribeVideo(DoneDelegate callback, long roomId, HashSet<long> uids, int timeout = 0);
	
	//-- Sync Method
	public int UnsubscribeVideo(long roomId, HashSet<long> uids, int timeout = 0);

Unsubscribe user video stream.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long roomId`

	RTC room ID.

+ `HashSet<long> uids`

	Uids of the unsubscribe target.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.

### Send P2P Request

	//-- Async Method
    public bool RequestP2PRTC(Action<long, int> callback, RTCP2PType type, long peerUID, int timeout = 0);
	
	//-- Sync Method
	public int RequestP2PRTC(out long callID, RTCP2PType type, long peerUID, int timeout = 0);

Send P2P request.

Parameters:

+ `Action<long, int> callback`

	Callabck for async method.  
    1. `long` is the call id of the P2P request.
	2. `int` is the error code indicating the calling is successful or the failed reasons.

+ `RTCP2PType type`

	RTC P2P type. Please refer [RTCP2PType](Structures.md#RTCP2PType)

+ `long peerUID`

	Uid of the peer.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.


### Cancel P2P Request

	//-- Async Method
    public bool CancelP2PRTC(DoneDelegate callback, long callID, int timeout = 0);
	
	//-- Sync Method
	public int CancelP2PRTC(long callID, int timeout = 0);

Cancel P2P request.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long callID`

	Call ID of the P2P request.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.


### Close P2P Request

	//-- Async Method
    public bool CloseP2PRTC(DoneDelegate callback, long callID, int timeout = 0);
	
	//-- Sync Method
	public int CloseP2PRTC(long callID, int timeout = 0);

Close P2P request.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long callID`

	Call ID of the P2P request.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.


### Accept P2P Request

	//-- Async Method
    public bool AcceptP2PRTC(DoneDelegate callback, long peerUid, RTCP2PType type, long callID, int timeout = 0);
	
	//-- Sync Method
	public int AcceptP2PRTC(long peerUid, RTCP2PType type, long callID, int timeout = 0);

Accept P2P request.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long peerUid`

	Uid of the peer.

+ `RTCP2PType type`

	RTC P2P type. Please refer [RTCP2PType](Structures.md#RTCP2PType)

+ `long callID`

	Call ID of the P2P request.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.


### Refuse P2P Request

	//-- Async Method
    public bool RefuseP2PRTC(DoneDelegate callback, long callID, int timeout = 0);
	
	//-- Sync Method
	public int RefuseP2PRTC(long callID, int timeout = 0);

Refuse P2P request.

Parameters:

+ `DoneDelegate callback`

	public delegate void DoneDelegate(int errorCode);

	Callabck for async method. Please refer [DoneDelegate](Delegates.md#DoneDelegate).

+ `long callID`

	Call ID of the P2P request.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async calling is start.
	* false: Start async calling is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means calling successed.

	Others are the reason for calling failed.


### Update Position

	//-- Async Method
	public bool UpdatePosition(DoneDelegate callback, long roomId, int x, int y, int z, int timeout = 0);
	
	//-- Sync Method
	public int UpdatePosition(long roomId, int x, int y, int z, int timeout = 0);

Update position.

Parameters:

+ `Action<int> callback`

	Callabck for async method.  
	1. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room ID.

+ `int x`

	x-coordinate

+ `int y`

	y-coordinate

+ `int z`

	z-coordinate

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.


### Set Voice Range

	//-- Async Method
	public bool SetVoiceRange(DoneDelegate callback, long roomId, int range, int timeout = 0);
	
	//-- Sync Method
	public int SetVoiceRange(long roomId, int range, int timeout = 0);

Set voice range.

Parameters:

+ `Action<int> callback`

	Callabck for async method.  
	1. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room ID.

+ `int range`

	The range of voice can be heard.

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.

### Set Max Receive Streams

	//-- Async Method
	public bool SetMaxReceiveStreams(DoneDelegate callback, long roomId, int maxReceiveStreams, int timeout = 0);
	
	//-- Sync Method
	public int SetMaxReceiveStreams(long roomId, int maxReceiveStreams, int timeout = 0);

Set max receive streams.

Parameters:

+ `Action<int> callback`

	Callabck for async method.  
	1. `int` is the error code indicating the calling is successful or the failed reasons.

+ `long roomId`

	RTC room ID.

+ `int maxReceiveStreams`

	Maximum number of received streams. 

+ `int timeout`

	Timeout in second.

	0 means using default setting.


Return Values:

+ bool for Async

	* true: Async sending is start.
	* false: Start async sending is failed.

+ int for Sync

	0 or com.fpnn.ErrorCode.FPNN_EC_OK means sending successed.

	Others are the reason for sending failed.