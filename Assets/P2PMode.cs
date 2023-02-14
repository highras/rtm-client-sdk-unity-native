using com.fpnn.rtm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class P2PMode : MonoBehaviour
{
    InputField UID_InputField;
    InputField Token_InputField;
    InputField Target_InputField;
    Text Push_Text;
    Toggle Microphone_Toggle;
    Toggle VoicePlay_Toggle;
    Toggle Camera_Toggle;
    Toggle SwitchCamera_Toggle;
    GameObject Self_RawImage;
    GameObject Target_RawImage;
    VideoSurface Self_VideoSurface = new VideoSurface();
    VideoSurface Target_VideoSurface = new VideoSurface();
    Text Log_Text;
    int lines = 0;

    long requestingCallID = 0;
    long currentCallID = 0;
    long currentUID = 0;
    long receiveCallID = 0;
    long receiveUID = 0;


    // Start is called before the first frame update
    void Start()
    {
        UID_InputField = GameObject.Find("UID_InputField").GetComponent<InputField>();
        Token_InputField = GameObject.Find("Token_InputField").GetComponent<InputField>();
        Target_InputField = GameObject.Find("Target_InputField").GetComponent<InputField>();
        Push_Text = GameObject.Find("Push_Image/Text").GetComponent<Text>();
        Microphone_Toggle = GameObject.Find("Microphone_Toggle").GetComponent<Toggle>();
        Microphone_Toggle.onValueChanged.AddListener(Microphone_Toggle_OnClick);
        VoicePlay_Toggle = GameObject.Find("VoicePlay_Toggle").GetComponent<Toggle>();
        VoicePlay_Toggle.onValueChanged.AddListener(VoicePlay_Toggle_OnClick);
        Camera_Toggle = GameObject.Find("Camera_Toggle").GetComponent<Toggle>();
        Camera_Toggle.onValueChanged.AddListener(Camera_Toggle_OnClick);
        SwitchCamera_Toggle = GameObject.Find("SwitchCamera_Toggle").GetComponent<Toggle>();
        SwitchCamera_Toggle.onValueChanged.AddListener(SwitchCamera_Toggle_OnClick);
        Self_RawImage = GameObject.Find("Self_RawImage");
        Self_VideoSurface = Self_RawImage.AddComponent<VideoSurface>();
        Target_RawImage = GameObject.Find("Target_RawImage");
        Target_VideoSurface = Target_RawImage.AddComponent<VideoSurface>();
        Log_Text = GameObject.Find("Log_ScrollView/Viewport/Content/Text").GetComponent<Text>();

        System.Random random = new System.Random();
        UID_InputField.text = random.Next(1000, 10000).ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Login_Button_OnClick()
    {
        long uid = Convert.ToInt64(UID_InputField.text);
        string token = Token_InputField.text;
        if (token == "")
            token = HomePage.GetToken(uid);
        example.common.RTMRTCQuestProcessor processor = new example.common.RTMRTCQuestProcessor();
        processor.PushP2PRTCRequestCallback = (long callId, long peerUid, RTCP2PType type) => { 
            ReceiveRequest(callId, peerUid, type); 
        };
        processor.PushP2PRTCEventCallback = (long callId, long peerUid, RTCP2PType type, RTCP2PEvent p2pEvent) => {
            ReceiveEvent(callId, peerUid, type, p2pEvent); 
        };
        HomePage.client = RTMClient.getInstance(HomePage.rtmEndpoint, HomePage.rtcEndpoint, HomePage.projectId, uid, processor);

        bool status = HomePage.client.Login((long pid_, long uid_, bool ok, int errorCode) =>
        {
            RTMControlCenter.callbackQueue.PostAction(() => 
            {
                Debug.Log("Async login " + ok + ". pid " + pid_ + ", uid " + uid_ + ", code : " + errorCode);
                if (ok)
                {
                    RTCEngine.ActiveRTCClient(HomePage.client);
                    AddLog(UID_InputField.text + " login succeed.");
                    Debug.Log("RTM login success.");
                }
                else
                {
                    AddLog(UID_InputField.text + " login failed.");
                    Debug.Log("RTM login failed, error code: " + errorCode);
                }
            });
        }, token);
        if (!status)
        {
            Debug.Log("Async login starting failed.");
            return;
        }
    }

    public void Request_Button_OnClick()
    {
        if (requestingCallID != 0)
        {
            AddLog("Can't send two request at the same time.");
            return;
        }
        long target = Convert.ToInt64(Target_InputField.text);
        int errorCode = HomePage.client.RequestP2PRTC(out long callID, RTCP2PType.Video, target);
        if (errorCode == com.fpnn.ErrorCode.FPNN_EC_OK)
        {
            requestingCallID = callID;
            AddLog("Sending request to " + target.ToString());
            Push_Text.text = "Sending request to " + target.ToString();
        }
        else
        {
            requestingCallID = 0;
            AddLog("RequestP2PRTC failed, target = " + target.ToString() + " errorCode = " + errorCode);
            Debug.Log("RequestP2PRTC failed, target = " + target.ToString() + " errorCode = " + errorCode);
        }
    }

    public void Cancel_Button_OnClick()
    {
        if (requestingCallID == 0 || currentCallID != 0)
        {
            AddLog("Can't cancel the request");
            return;
        }

        int errorCode = HomePage.client.CancelP2PRTC(requestingCallID);
        if (errorCode == com.fpnn.ErrorCode.FPNN_EC_OK)
        {
            requestingCallID = 0;
            Push_Text.text = "";
            AddLog("Cancel the request to " + currentUID.ToString());
        }
        else
        {
            Debug.Log("CancelP2PRTC callID = " + requestingCallID.ToString() + ", errorCode = " + errorCode.ToString());
        }
    }

    public void Accept_Button_OnClick()
    {
        if (receiveCallID == 0)
        {
            AddLog("Didn't receive a request.");
            return;
        }
        int errorCode = HomePage.client.AcceptP2PRTC(receiveUID, RTCP2PType.Video, receiveCallID);
        if (errorCode == com.fpnn.ErrorCode.FPNN_EC_OK)
        {
            AddLog("Accept the request from " + receiveUID.ToString());
            currentCallID = receiveCallID;
            currentUID = receiveUID;
            requestingCallID = 0;
            Push_Text.text = currentUID.ToString() + " is calling";
            if (Target_VideoSurface.Uid() == 0)
                Target_VideoSurface.SetVideoInfo(currentUID, false);
            Microphone_Toggle.isOn = true;
            VoicePlay_Toggle.isOn = true;
        }
        else
        {
            Debug.Log("AcceptP2PRTC callID = " + receiveCallID.ToString() + ", errorCode = " + errorCode.ToString());
        }
    }

    public void Refuse_Button_OnClick()
    {
        if (receiveCallID == 0)
        {
            AddLog("Didn't receive a request.");
            return;
        }

        int errorCode = HomePage.client.RefuseP2PRTC(receiveCallID);
        if (errorCode == com.fpnn.ErrorCode.FPNN_EC_OK)
        {
            AddLog("Refuse the request from " + receiveUID.ToString());
            receiveCallID = 0;
            receiveUID = 0;
            Push_Text.text = "";
        }
        else
        {
            Debug.Log("RefuseP2PRTC callID = " + receiveCallID.ToString() + ", errorCode = " + errorCode.ToString());
        }
    }

    public void Microphone_Toggle_OnClick(bool isOn)
    {
        if (isOn)
        {
            RTCEngine.OpenMicroPhone();
        }
        else
        {
            RTCEngine.CloseMicroPhone();
        }
    }

    public void VoicePlay_Toggle_OnClick(bool isOn)
    {
        if (isOn)
        {
            RTCEngine.OpenVoicePlay();
        }
        else
        {
            RTCEngine.CloseVoicePlay();
            Microphone_Toggle_OnClick(false);
        }
    }

    public void Camera_Toggle_OnClick(bool isOn)
    {
        if (isOn)
        {
            RTCEngine.OpenCamera();
            if (Self_VideoSurface.Uid() != 0)
                return;
            Self_VideoSurface.SetVideoInfo(HomePage.client.Uid, true);
        }
        else
        {
            RTCEngine.CloseCamera();
            Self_VideoSurface.ClearVideoInfo();
        }
    }

    public void SwitchCamera_Toggle_OnClick(bool isOn)
    {
        RTCEngine.SwitchCamera();
    }

    public void Close_Button_OnClick()
    {
        if (currentCallID == 0)
        {
            //Tip_Text.text = "无法挂断通话";
            return;
        }

        int errorCode = HomePage.client.CloseP2PRTC(currentCallID);
        if (errorCode == com.fpnn.ErrorCode.FPNN_EC_OK)
        {
            currentCallID = 0;
            currentUID = 0;
            requestingCallID = 0;
            Push_Text.text = "";
            Microphone_Toggle.isOn = false;
            VoicePlay_Toggle.isOn = false;
            Camera_Toggle.isOn = false;
            Target_VideoSurface.ClearVideoInfo();
        }
        else
        {
            Debug.Log("CloseP2PRTC callID = " + currentCallID + ", errorCode = " + errorCode);
        }
    }

    void ReceiveRequest(long callId, long peerUid, RTCP2PType type)
    {
        receiveCallID = callId;
        receiveUID = peerUid;
        Push_Text.text = peerUid.ToString() + " is requesting";
        AddLog("ReceiveRequest callId = " + callId + ", peerUid = " + peerUid + ", type = " + type);
    }

    void ReceiveEvent(long callId, long peerUid, RTCP2PType type, RTCP2PEvent p2pEvent)
    {
        //Cancel = 1,     //对端取消p2p请求
        //Close = 2,      //对端挂断
        //Accpet = 3,    //对端已经接受p2p请求
        //Refuse = 4,     //对端拒绝p2p请求
        //NoAnswer = 5,   //对端无人接听
        AddLog("ReceiveEvent callId = " + callId + ", peerUid = " + peerUid + ", type = " + type + ", p2pEvent = " + p2pEvent);
        if (p2pEvent == RTCP2PEvent.Cancel)
        {
            receiveCallID = 0;
            receiveUID = 0;
            requestingCallID = 0;
            Push_Text.text = "";
        }
        else if (p2pEvent == RTCP2PEvent.Close)
        {
            receiveCallID = 0;
            receiveUID = 0;
            currentCallID = 0;
            currentUID = 0;
            Microphone_Toggle.isOn = false;
            VoicePlay_Toggle.isOn = false;
            Camera_Toggle.isOn = false;
            Target_VideoSurface.ClearVideoInfo();
            Push_Text.text = "";
        }
        else if (p2pEvent == RTCP2PEvent.Accpet)
        {
            currentCallID = callId;
            currentUID = peerUid;
            requestingCallID = 0;
            Push_Text.text = peerUid.ToString() + " is calling";
            if (Target_VideoSurface.Uid() == 0)
                Target_VideoSurface.SetVideoInfo(currentUID, false);
            Microphone_Toggle.isOn = true;
            VoicePlay_Toggle.isOn = true;
        }
        else if (p2pEvent == RTCP2PEvent.Refuse)
        {
            requestingCallID = 0;
            Push_Text.text = "";
        }
        else if (p2pEvent == RTCP2PEvent.NoAnswer)
        {
            requestingCallID = 0;
            Push_Text.text = "";
        }
    }

    public void Return_Button_OnClick()
    {
        SceneManager.LoadScene("HomePage");
    }

    void AddLog(string log)
    {
        lines += 1;       
        if (lines > 30)
        {
            Log_Text.text = Log_Text.text.Substring(Log_Text.text.IndexOf("\n")+1);
        }

        if (Log_Text.text == "")
            Log_Text.text = log;
        else
            Log_Text.text += "\n" + log;
    }
}
