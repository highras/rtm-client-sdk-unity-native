using System;
using System.Collections.Generic;
using System.Threading;
using com.fpnn.rtm;
using UnityEngine;

namespace example.common
{
    class RTMRTCQuestProcessor : RTMQuestProcessor
    {

        //----------------[ System Events ]-----------------//

        public override void SessionClosed(int ClosedByErrorCode)
        {
            lock (this)
            {
                Debug.Log($"Session closed by error code: {ClosedByErrorCode}");
            }

        }

        public override bool ReloginWillStart(int lastErrorCode, int retriedCount)
        {
            lock (this)
            {
                Debug.Log($"Relogin will start. Last error code is {lastErrorCode}, total relogin count is {retriedCount}.");
            }

            return true;
        }

        public override void ReloginCompleted(bool successful, bool retryAgain, int errorCode, int retriedCount)
        {
            lock (this)
            {
                if (successful)
                {
                    Debug.Log("Relogin Completed. Relogin succeeded, total relogin count is " + retriedCount);
                }
                else
                {
                    Debug.Log($"Relogin Completed. Relogin failed, error code: {errorCode}, will"
                        + (retryAgain ? "" : " not") + $" retry again. Total relogin count is {retriedCount}.");
                }
            }
        }


        public override void Kickout()
        {
            lock (this)
                Debug.Log("Received kickout.");
        }

        public override void KickoutRoom(long roomId)
        {
            lock (this)
                Debug.Log($"Kickout from room {roomId}");
        }

        public override void PushEnterRTCRoom(long roomId, long uid, long mtime)
        {
            lock (this)
            {
                Debug.Log("Receive push enter rtc room, roomId = " + roomId + ", uid = " + uid + ", mtime = " + mtime);
            }
        }

        public override void PushExitRTCRoom(long roomId, long uid, long mtime)
        {
            lock (this)
            {
                Debug.Log("Receive push exit rtc room, roomId = " + roomId + ", uid = " + uid + ", mtime = " + mtime);
            }
        }

        public override void PushRTCRoomClosed(long roomId)
        {
            lock (this)
            {
                Debug.Log("Receive push rtc room closed, roomId = " + roomId);
            }
        }

        public override void PushInviteIntoRTCRoom(long fromUid, long roomId)
        {
            lock (this)
            {
                Debug.Log("Receive push invite into rtc room, fromUid = " + fromUid + ", roomId = " + roomId);
            }
        }

        public override void PushKickOutRTCRoom(long fromUid, long roomId)
        {
            lock (this)
            {
                Debug.Log("Receive push kickout rtc room, fromUid = " + fromUid + ", roomId = " + roomId);
            }
        }

        public override void PushPullIntoRTCRoom(long roomId, string token)
        {
            lock (this)
            {
                Debug.Log("Receive push pull into rtc room, roomId = " + roomId + ", token = " + token);
            }
        }

        public override void PushAdminCommand(RTCAdminCommand command, HashSet<long> uids)
        {
            lock (this)
            {
                string uidStr = "[";
                foreach (var uid in uids)
                {
                    uidStr += " " + uid;
                }
                uidStr += " ]";
                Debug.Log("Receive push admin command, command = " + command + ", uids = " + uidStr);
            }
        }

        public override void PushP2PRTCRequest(long callId, long peerUid, RTCP2PType type)
        {
            lock (this)
            {
                Debug.Log("Receive push p2p rtc request callId = " + callId + ", peerUid = " + peerUid + ", type = " + type);
            }
        }

        public override void PushP2PRTCEvent(long callId, long peerUid, RTCP2PType type, RTCP2PEvent p2pEvent)
        {
            lock (this)
            {
                Debug.Log("Receive push p2p rtc event callId = " + callId + ", peerUid = " + peerUid + ", type = " + type + ", p2pEvent = " + p2pEvent);
            }
        }
    }
}
