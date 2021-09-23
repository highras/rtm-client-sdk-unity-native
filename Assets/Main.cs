﻿using System.Threading;
using UnityEngine;
using com.fpnn;
using com.fpnn.rtm;

public class Main : MonoBehaviour
{
    public interface ITestCase
    {
        void Start(string endpoint, long pid, long uid, string token);
        void Stop();
    }

    private string rtmServerEndpoint = "161.189.171.91:13321";
    private long pid = 11000001;
    private long uid = 7654321;
    private string token = "D3A076E1CCAFD3CB01CB7C8D778153BB";

    Thread testThread;
    ITestCase tester;

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

        testThread = new Thread(TestMain)
        {
            IsBackground = true
        };
        testThread.Start();

        /*
            This is a temporary version of the test code, because the audio-related functions require running on the main thread, so add it here
        */
        // tester = new Audios();
        // tester.Start(rtmServerEndpoint, pid, uid, token);
    }

    void TestMain()
    {
        //-- Examples
        // tester = new Chat();
        // tester = new Data();
        // tester = new Files();
        // tester = new Friends();
        // tester = new Groups();
        // tester = new Histories();
        // tester = new Login();
        // tester = new Messages();
        // tester = new Rooms();
        // tester = new RTMSystem();
        tester = new Users();
        //tester = new RTC();

        tester.Start(rtmServerEndpoint, pid, uid, token);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnApplicationQuit()
    {
        tester.Stop();
        testThread.Join();
        Debug.Log("Test App exited.");
#if UNITY_EDITOR
        //ClientManager.Stop();
        RTCEngine.Stop();
        ClientEngine.Close();
        //Client.closeEngine();
#endif
    }
}