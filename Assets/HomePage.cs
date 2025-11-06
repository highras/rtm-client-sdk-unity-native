using com.fpnn;
using com.fpnn.proto;
using com.fpnn.rtm;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomePage : MonoBehaviour
{
    public static long projectId = 0;
    public static string rtmEndpoint = "";
    public static RTMClient client = null;
    public static long uid = 0;
    public static string token = "";

    public static string GetToken(long uid)
    {
        return "";
    }

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        com.fpnn.common.ErrorRecorder RerrorRecorderecorder = new ErrorRecorder();
        Config config = new Config
        {
            errorRecorder = RerrorRecorderecorder
        };
        ClientEngine.Init(config);

        RTMConfig rtmConfig = new RTMConfig()
        {
            defaultErrorRecorder = RerrorRecorderecorder
        };
        RTMControlCenter.Init(rtmConfig);
        RTCEngine.Init();
    }

    void OnApplicationQuit()
    {
        RTCEngine.Stop();
        RTMControlCenter.Close();
        ClientEngine.Close();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RoomMode_Button_OnClick()
    {
        SceneManager.LoadScene("RoomMode");
    }

    public void P2PMode_Button_OnClick()
    {
        SceneManager.LoadScene("P2PMode");
    }
}
