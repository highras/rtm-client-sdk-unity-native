using UnityEngine;
using System.Collections;
using com.fpnn.rtm;
using System.Threading;
using System.Collections.Generic;

public class RTC : Main.ITestCase
{
    private RTMClient client;
    private RTMClient client2;

    public void Start(string endpoint, long pid, long uid, string token)
    {
        client = LoginRTM(endpoint, pid, uid, token);
        client2 = LoginRTM(endpoint, pid, uid + 1, "127129F5586DC92DB6EE6A861D81C966");
        if (client == null || client2 == null)
        {
            Debug.Log("User " + uid + " login RTM failed.");
            return;
        }
        RTCEngine.ActiveRTCClient(client);
        client.CreateRTCRoom((long roomId, int errorCode) =>
        {
            Debug.Log("CreateRTCRoom roomId = " + roomId + ", errorCode = " + errorCode);
            if (errorCode == com.fpnn.ErrorCode.FPNN_EC_OK)
            {
                RTCEngine.setActiveRoomId(roomId);
                client.OpenMicroPhone();
                client.OpenVoicePlay();
            }
            else
            {
                client.EnterRTCRoom((long tmpRoomId, RTCRoomType roomType, int tmpErrorCode) =>
                {
                    Debug.Log("EnterRTCRoom roomId = " + tmpRoomId + ", errorCode = " + tmpErrorCode);
                    RTCEngine.setActiveRoomId(tmpRoomId);
                    client.OpenVoicePlay();
                    client.OpenMicroPhone();
                }, 666);
            }
        }, 666, RTCRoomType.VoiceRoom);
        Thread.Sleep(1000);
        client.InviteUserIntoRTCRoom((int errorCode) =>
        {
            Debug.Log("InviteUserIntoRTCRoom errorCode = " + errorCode);
        }, 666, new HashSet<long> { uid + 1 });
        client2.EnterRTCRoom((long roomId, RTCRoomType roomType, int errorCode) =>
        {
            Debug.Log("EnterRTCRoom roomId = " + roomId + ", errorCode = " + errorCode);
        }, 666);
        Thread.Sleep(100);
        client.AdminCommand((int errorCode) =>
        {
            Debug.Log("AdminCommand errorCode = " + errorCode);
        }, 666, new HashSet<long> { uid + 1 }, RTCAdminCommand.AppointAdministrator);
        client.GetRTCRoomMemberCount((int count, int errorCode) =>
        {
            Debug.Log("GetRTCRoomMemberCount count = " + count + ", errorCode = " + errorCode);
        }, 666);

        client.GetRTCRoomMembers((HashSet<long> uids, HashSet<long> administrators, long owner, int errorCode) =>
        {
            if (errorCode != com.fpnn.ErrorCode.FPNN_EC_OK)
            {
                Debug.Log("GetRTCRoomMemberCount errorCode = " + errorCode);
                return;
            }
            Debug.Log("GetRTCRoomMemberCount owner = " + owner + ", errorCode = " + errorCode);
            foreach (var uidTmp in uids)
                Debug.Log("--Room Member = " + uidTmp);
            foreach (var administrator in administrators)
                Debug.Log("--Room Administrators = " + administrator);
        }, 666);
        Thread.Sleep(100);
        client2.ExitRTCRoom((int errorCode) =>
        {
            Debug.Log("ExitRTCRoom errorCode = " + errorCode);
        }, 666);

        Thread.Sleep(1000);
        Debug.Log("============== Demo completed ================");
    }

    public void Stop() { }

    static RTMClient LoginRTM(string rtmEndpoint, long pid, long uid, string token)
    {
        RTMClient client = new RTMClient(rtmEndpoint, "rtc-nx-front.ilivedata.com:13702", pid, uid, new example.common.RTMExampleQuestProcessor());

        int errorCode = client.Login(out bool ok, token);
        if (ok)
        {
            Debug.Log("RTM login success.");
            return client;
        }
        else
        {
            Debug.Log("RTM login failed, error code: " + errorCode);
            return null;
        }
    }
}
