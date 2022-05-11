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
    public static long projectId = 11000002;

    public static string token = "";

    public static RTMClient client = null;

    private static string GetMD5(string str, bool upper)
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes(str);
        return GetMD5(inputBytes, upper);
    }

    private static string GetMD5(byte[] bytes, bool upper)
    {
        MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(bytes);
        string f = "x2";

        if (upper)
        {
            f = "X2";
        }

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString(f));
        }

        return sb.ToString();
    }

    private static long GenerateSalt()
    {
        return MidGenerator.Gen();
    }

    static string secretKey = "f5a45c68-2279-4de7-b00e-aa10287531a8";
    private static Quest GenerateQuest(string cmd)
    {
        long ts = ClientEngine.GetCurrentSeconds();
        long salt = GenerateSalt();
        string sign = GetMD5(projectId + ":" + secretKey + ":" + salt + ":" + cmd + ":" + ts, true);
        Quest quest = new Quest(cmd);
        quest.Param("pid", projectId);
        quest.Param("salt", salt);
        quest.Param("sign", sign);
        quest.Param("ts", ts);
        return quest;
    }

    public static string GetToken(long uid)
    {
        Quest quest = GenerateQuest("gettoken");
        quest.Param("uid", uid);
        quest.Param("version", "csharp-" + RTMConfig.SDKVersion);
        TCPClient client = TCPClient.Create("161.189.171.91:13315", true);
        client.SetQuestProcessor(new RTMMasterProcessor());

        Answer answer = client.SendQuest(quest);
        string token = answer.Get<string>("token", null);
        return token;
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
        RTCEngine.Init();

        RTMConfig rtmConfig = new RTMConfig()
        {
            defaultErrorRecorder = RerrorRecorderecorder
        };
        RTMControlCenter.Init(rtmConfig);
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
