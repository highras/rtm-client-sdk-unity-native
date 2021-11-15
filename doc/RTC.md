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