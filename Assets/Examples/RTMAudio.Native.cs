using System.Collections.Generic;
using UnityEngine;
using com.fpnn.rtm;

class MyAudioRecorderListener : AudioRecorderNative.IAudioRecorderListener {    
    public void RecordStart(bool success)
    { 
        Debug.Log("RecordStart success = " + success);
    }

    public void RecordEnd()
    { 
        Debug.Log("RecordEnds");
    }

    public void OnRecord(RTMAudioData audioData)
    {
        Debug.Log("OnRecord " + audioData.Duration);
        AudioRecorderNative.Instance.Play(audioData);
    }

    public void OnVolumn(double db)
    { 
        Debug.Log("OnVolumn db=" + db);
    }

    public void PlayEnd()
    { 
        Debug.Log("PlayEnd");
    }
}

class AudioNative : Main.ITestCase
{

    public void Start(string endpoint, long pid, long uid, string token)
    {
        AudioRecorderNative.Instance.Init("zh-CN", new MyAudioRecorderListener());
        AudioRecorderNative.Instance.StartRecord();

        //System.Threading.Thread.Sleep(10 * 1000);

        Debug.Log("============== Demo completed ================");
    }

    public void Stop()
    {

    }

}