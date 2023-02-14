using com.fpnn.rtm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomMode : MonoBehaviour
{
    InputField UID_InputField;
    InputField Token_InputField;
    InputField RoomID_InputField;
    Text ActiveRoom_Text;
    Dropdown ActiveRoom_Dropdown;
    Dropdown Subscribe_Dropdown;
    Text Log_Text;
    Toggle Microphone_Toggle;
    Toggle VoicePlay_Toggle;
    Toggle Camera_Toggle;
    Toggle SwitchCamera_Toggle;
    GameObject Self_RawImage;
    GameObject Target1_RawImage;
    GameObject Target2_RawImage;
    GameObject Target3_RawImage;
    GameObject Target4_RawImage;
    VideoSurface[] VideoSurfaceList = new VideoSurface[5];

    //List<string> roomList = new List<string>();
    //int activeRoomIndex = -1;
    //List<string> roomMemberList = new List<string>();
    //int roomMemberIndex = -1;
    long activeRoomId = 0;
    int lines = 0;
    // Start is called before the first frame update
    void Start()
    {
        UID_InputField = GameObject.Find("UID_InputField").GetComponent<InputField>();
        Token_InputField = GameObject.Find("Token_InputField").GetComponent<InputField>();
        RoomID_InputField = GameObject.Find("RoomID_InputField").GetComponent<InputField>();
        ActiveRoom_Text = GameObject.Find("ActiveRoom_Image/Text").GetComponent<Text>();
        ActiveRoom_Dropdown = GameObject.Find("ActiveRoom_Dropdown").GetComponent<Dropdown>();
        Subscribe_Dropdown = GameObject.Find("Subscribe_Dropdown").GetComponent<Dropdown>();
        Log_Text = GameObject.Find("Log_ScrollView/Viewport/Content/Text").GetComponent<Text>();
        Microphone_Toggle = GameObject.Find("Microphone_Toggle").GetComponent<Toggle>();
        Microphone_Toggle.onValueChanged.AddListener(Microphone_Toggle_OnClick);
        VoicePlay_Toggle = GameObject.Find("VoicePlay_Toggle").GetComponent<Toggle>();
        VoicePlay_Toggle.onValueChanged.AddListener(VoicePlay_Toggle_OnClick);
        Camera_Toggle = GameObject.Find("Camera_Toggle").GetComponent<Toggle>();
        Camera_Toggle.onValueChanged.AddListener(Camera_Toggle_OnClick);
        SwitchCamera_Toggle = GameObject.Find("SwitchCamera_Toggle").GetComponent<Toggle>();
        SwitchCamera_Toggle.onValueChanged.AddListener(SwitchCamera_Toggle_OnClick);
        Self_RawImage = GameObject.Find("Self_RawImage");
        VideoSurface selfVideoSurface = Self_RawImage.AddComponent<VideoSurface>();
        VideoSurfaceList[0] = selfVideoSurface;
        Target1_RawImage = GameObject.Find("Target1_RawImage");
        VideoSurface target1VideoSurface = Target1_RawImage.AddComponent<VideoSurface>();
        VideoSurfaceList[1] = target1VideoSurface;
        Target2_RawImage = GameObject.Find("Target2_RawImage");
        VideoSurface target2VideoSurface = Target2_RawImage.AddComponent<VideoSurface>();
        VideoSurfaceList[2] = target2VideoSurface;
        Target3_RawImage = GameObject.Find("Target3_RawImage");
        VideoSurface target3VideoSurface = Target3_RawImage.AddComponent<VideoSurface>();
        VideoSurfaceList[3] = target3VideoSurface;
        Target4_RawImage = GameObject.Find("Target4_RawImage");
        VideoSurface target4VideoSurface = Target4_RawImage.AddComponent<VideoSurface>();
        VideoSurfaceList[4] = target4VideoSurface;

        System.Random random = new System.Random();
        UID_InputField.text = random.Next(1000, 10000).ToString();
        RoomID_InputField.text = "666";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void AddLog(string log)
    {
        lines += 1;
        if (lines > 25)
        {
            Log_Text.text = Log_Text.text.Substring(Log_Text.text.IndexOf("\n") + 1);
        }
        if (Log_Text.text == "")
            Log_Text.text = log;
        else
            Log_Text.text += "\n" + log;
    }

    public void Login_Button_OnClick()
    {
        long uid = Convert.ToInt64(UID_InputField.text);
        string token = Token_InputField.text;
        if (token == "")
            token = HomePage.GetToken(uid);
        example.common.RTMRTCQuestProcessor processor = new example.common.RTMRTCQuestProcessor();
        processor.PushEnterRTCRoomCallback = (long roomId_, long uid_, long mtime_, string nickName) => {
            Debug.Log("pushEnterRTCRoomCallback");
            Update_Subscribe_Dropdown(); 
        };

        processor.PushExitRTCRoomCallback = (long roomId_, long uid_, long mtime_) => { 
            Debug.Log("pushExitRTCRoomCallback");
            Update_Subscribe_Dropdown();
            DisableVideoSurface(uid_);
        };

        processor.SessionClosedCallback = (int ClosedByErrorCode) =>
        {
            AddLog("Session Closed, ErrorCode = " + ClosedByErrorCode);
        };

        processor.ReloginCompletedCallback = (bool successful, bool retryAgain, int errorCode, int retriedCount) =>
        {
            if (!successful)
                return;
            if (activeRoomId == 0)
                return;
            HomePage.client.EnterRTCRoom((long room, RTCRoomType type, int errorCode) =>
            {
                Debug.Log("EnterRTCRoom errorCode=" + errorCode);
                RTMControlCenter.callbackQueue.PostAction(() =>
                {
                    if (errorCode == 0)
                    {
                        AddLog("Enter room " + activeRoomId);
                        Update_AcitveRoom_Dropdown();
                    }
                    else
                    {
                        HomePage.client.CreateRTCRoom((long room2, int errorCode2) =>
                        {
                            Debug.Log("CreateRTCRoom errorCode=" + errorCode2);
                            if (errorCode == 0)
                            {
                                AddLog("Enter room " + activeRoomId);
                                Update_AcitveRoom_Dropdown();
                            }
                            else
                            {

                            }
                        }, activeRoomId, RTCRoomType.VideoRoom);
                    }
                });
            }, activeRoomId);
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
                    Debug.Log("RTM login success.");
                    AddLog("User " + UID_InputField.text + " login succeed.");
                }
                else
                {
                    //Tips_Text.text = UID_InputField.text + " 登陆失败";
                    Debug.Log("RTM login failed, error code: " + errorCode);
                    AddLog("User " + UID_InputField.text + "login failed.");
                }
            });
        }, token);
        if (!status)
        {
            //Tips_Text.text = UID_InputField.text + " 登陆连接失败";
            Debug.Log("Async login starting failed.");
            return;
        }
    }

    public void EnterRoom_Button_OnClick()
    {
        if (HomePage.client == null)
            return;
        long roomID = Convert.ToInt64(RoomID_InputField.text);
        HomePage.client.EnterRTCRoom((long room, RTCRoomType type, int errorCode) =>
        {
            RTMControlCenter.callbackQueue.PostAction(() =>
            {
                Debug.Log("EnterRTCRoom errorCode=" + errorCode);
                if (errorCode == 0)
                {
                    //Tips_Text.text = "加入房间" + RoomID_InputField.text + "成功";
                    AddLog("Enter room " + RoomID_InputField.text);
                    Update_AcitveRoom_Dropdown();
                }
                else
                {
                    HomePage.client.CreateRTCRoom((long room2, int errorCode2) =>
                    {

                        RTMControlCenter.callbackQueue.PostAction(() =>
                        {
                            Debug.Log("CreateRTCRoom errorCode=" + errorCode2);
                            if (errorCode2 == 0)
                            {
                                AddLog("Enter room " + RoomID_InputField.text);
                                Update_AcitveRoom_Dropdown();
                            }
                            else
                            {

                            }
                        });
                    }, roomID, RTCRoomType.VideoRoom);
                    //Tips_Text.text = "加入房间" + RoomID_InputField.text + "失败";
                }
            });
        }, roomID);
    }

    public void ExitRoom_Button_OnClick()
    {
        if (HomePage.client == null)
            return;
        long roomID = Convert.ToInt64(RoomID_InputField.text);
        HomePage.client.ExitRTCRoom((int errorCode) => {
            RTMControlCenter.callbackQueue.PostAction(() =>
            {
                if (errorCode == 0)
                {
                    Microphone_Toggle.isOn = false;
                    VoicePlay_Toggle.isOn = false;
                    Update_AcitveRoom_Dropdown();
                    AddLog("Exit room " + RoomID_InputField.text);
                    if (roomID == activeRoomId)
                        activeRoomId = 0;
                }
            });
        }, roomID);
    }

    void Update_AcitveRoom_Dropdown()
    {
        List<long> rtcRoomList = HomePage.client.GetRTCRoomList();
        ActiveRoom_Dropdown.ClearOptions();
        List<string> roomList = new List<string>();
        foreach (long roomId in rtcRoomList)
            roomList.Add(roomId.ToString());
        ActiveRoom_Dropdown.AddOptions(roomList);
        //if (roomList.Count > 0)
        //{
        //    ActiveRoom_Dropdown.captionText.text = roomList[0];
        //    activeRoomIndex = 0;
        //}
        //else
        //{
        //    ActiveRoom_Dropdown.captionText.text = "";
        //    activeRoomIndex = -1;
        //}
    }

    public void ActiveRoom_Dropdown_OnValueChange(int index)
    {
        //activeRoomIndex = index;
        //ActiveRoom_Dropdown.captionText.text = roomList[activeRoomIndex];
    }

    public void ActiveRoom_Button_OnClick()
    {
        if (ActiveRoom_Dropdown.value < 0)
            return;
        long roomID = Convert.ToInt64(ActiveRoom_Dropdown.options[ActiveRoom_Dropdown.value].text);
        RTCEngine.SetActiveRoomId(roomID);
        ActiveRoom_Text.text = "Acitve Room " + roomID;
        AddLog("Set active room " + roomID);
        Microphone_Toggle.isOn = false;
        VoicePlay_Toggle.isOn = true;
        activeRoomId = roomID;
        Update_Subscribe_Dropdown();
    }

    public void Microphone_Toggle_OnClick(bool isOn)
    {
        Debug.Log("Microphone_Toggle_OnClick " + isOn);
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
        Debug.Log("VoicePlay_Toggle_OnClick " + isOn);
        if (isOn)
        {
            RTCEngine.OpenVoicePlay();
        }
        else
        {
            RTCEngine.CloseVoicePlay();
        }
    }

    public void Camera_Toggle_OnClick(bool isOn)
    {
        Debug.Log("Camera_Toggle_OnClick " + isOn);
        if (isOn)
        {
            RTCEngine.OpenCamera();
            EnableVideoSurface(HomePage.client.Uid);
        }
        else
        {
            RTCEngine.CloseCamera();
            DisableVideoSurface(HomePage.client.Uid);
        }
    }

    public void SwitchCamera_Toggle_OnClick(bool isOn)
    {
        Debug.Log("SwitchCamera_Toggle_OnClick " + isOn);
        RTCEngine.SwitchCamera();
    }

    void Update_Subscribe_Dropdown()
    {
        if (activeRoomId == 0)
            return;
        HomePage.client.GetRTCRoomMembers(out HashSet<long> members, out _, out _, activeRoomId);
        Subscribe_Dropdown.ClearOptions();
        List<string> roomMemberList = new List<string>();
        foreach (long uid in members)
        {
            if (uid != HomePage.client.Uid)
                roomMemberList.Add(uid.ToString());
        }
        Subscribe_Dropdown.AddOptions(roomMemberList);
    }

    public void Subscribe_Dropdown_OnValueChange(int index)
    {
    }


    bool CheckVideoSurface()
    {
        for (int i = 1; i <= 4; ++i)
        {
            if (VideoSurfaceList[i].Uid() == 0)
                return true;
        }
        return false;
    }

    void EnableVideoSurface(long uid)
    {
        if (uid == HomePage.client.Uid)
        {
            if (VideoSurfaceList[0].Uid() != 0)
                return;
            VideoSurfaceList[0].SetVideoInfo(uid, true);
        }
        else
        {
            for (int i = 1; i <= 4; ++i)
            {
                if (VideoSurfaceList[i].Uid() == 0)
                {
                    VideoSurfaceList[i].SetVideoInfo(uid, false);
                    return;
                }
            }
        }
    }

    void DisableVideoSurface(long uid)
    {
        if (uid == HomePage.client.Uid)
        {
            VideoSurfaceList[0].ClearVideoInfo();
        }
        else
        {
            for (int i = 1; i <= 4; ++i)
            {
                if (VideoSurfaceList[i].Uid() == uid)
                {
                    VideoSurfaceList[i].ClearVideoInfo();
                    return;
                }
            }
        }
    }

    public void Subscribe_Button_OnClick()
    {
        if (Subscribe_Dropdown.value < 0)
            return;
        long memberId = Convert.ToInt64(Subscribe_Dropdown.options[Subscribe_Dropdown.value].text);
        if (CheckVideoSurface() == false)
        {
            AddLog("Can't subscribe more members.");
            return;
        }
        HomePage.client.SubscribeVideo((int errorCode) => {
            RTMControlCenter.callbackQueue.PostAction(() =>
            {
                if (errorCode == 0)
                {
                    EnableVideoSurface(memberId);
                    AddLog("Subscribe " + memberId + " succeed.");
                    Debug.Log("Subscribe " + memberId + " succeed.");
                }
                else
                {
                    Debug.Log("Subscribe " + memberId + " failed.");
                }
            });
        }, activeRoomId, new HashSet<long> { memberId });
    }

    public void Unsubscribe_Button_OnClick()
    {
        if (Subscribe_Dropdown.value < 0)
            return;
        long memberId = Convert.ToInt64(Subscribe_Dropdown.options[Subscribe_Dropdown.value].text);
        HomePage.client.UnsubscribeVideo((int errorCode) => {
            RTMControlCenter.callbackQueue.PostAction(() =>
            {
                if (errorCode == 0)
                {
                    DisableVideoSurface(memberId);
                    AddLog("Unsubscribe " + memberId + " succeed.");
                    Debug.Log("Unsubscribe " + memberId + " succeed.");
                }
                else
                {
                    Debug.Log("Unsubscribe " + memberId + " failed.");
                }
            });
        }, activeRoomId, new HashSet<long> { memberId });
    }

    public void Return_Button_OnClick()
    {
        SceneManager.LoadScene("HomePage");
    }
}
