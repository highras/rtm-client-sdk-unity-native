using UnityEngine;
using System.Collections;
using com.fpnn.rtm;
using System.Threading;
using System.Collections.Generic;

public class RTCP2P : Main.ITestCase
{
    private static RTMClient client;
    private static RTMClient client2;

    private static string rtcEndpoint = "161.189.171.91:13702";
    private static long uid2 = 1234567;
    private static string token2 = "0D42B5BA292847E349F049047B6BC3881B1E7555E2EA48CE08F9269";

    public void Start(string endpoint, long pid, long uid, string token)
    {
        client = LoginRTM(endpoint, rtcEndpoint, pid, uid, token);
        client2 = LoginRTM(endpoint, rtcEndpoint, pid, uid2, token2);

        Thread.Sleep(1000);

        client.RequestP2PRTC((long callId, int errorCode) => {
            if (errorCode == com.fpnn.ErrorCode.FPNN_EC_OK)
            { 
                Debug.Log("RequestP2PRTC callId = " + callId + " succeed");
                //client.CancelP2PRTC((int errorCode2) => {
                //    Debug.Log("CancelP2PRTC errorCode = " + errorCode2);
                //}, callId);
            }
            else
            {
                Debug.Log("RequestP2PRTC errorCode = " + errorCode);
            }
        }, RTCP2PType.Voice, uid2);
    }

    public void Stop() { }

    static RTMClient LoginRTM(string rtmEndpoint, string rtcEndpoint, long pid, long uid, string token)
    {
        RTMClient client = RTMClient.getInstance(rtmEndpoint,pid, uid, new example.common.RTMExampleQuestProcessor());

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

    public static void Refuse(long callId)
    {
        client2.RefuseP2PRTC((int errorCode) => {
            Debug.Log("RefuseP2PRTC errorCode = " + errorCode);
        }, callId);
    }

    public static void Accpet(long callId, long peerUid, RTCP2PType type)
    {
        client2.AcceptP2PRTC((int errorCode) =>
        {
            Debug.Log("AcceptP2PRTC errorCode = " + errorCode);
        }, peerUid, type, callId);
    }

    public static void Close(long callId)
    {
        bool status = client.CloseP2PRTC((int errorCode) =>
        {
            Debug.Log("CloseP2PRTC errorCode = " + errorCode);
        }, callId);
        Debug.Log("CloseP2PRTC status = " + status);
    }
}
