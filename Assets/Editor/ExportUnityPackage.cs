using System.Collections;
using System.Collections.Generic;
using com.fpnn;
using com.fpnn.rtm;
using UnityEngine;
using UnityEditor;

public class ExportUnityPackage : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    [MenuItem("AssetDatabase/Export")]
    static void Export()
    {
        var exportedPackageAssetList = new List<string>();
        exportedPackageAssetList.Add("Assets/Examples");
        exportedPackageAssetList.Add("Assets/Main.cs");
        exportedPackageAssetList.Add("Assets/ErrorRecorder.cs");
        exportedPackageAssetList.Add("Assets/RTMExampleQuestProcessor.cs");
        exportedPackageAssetList.Add("Assets/Scenes/SampleScene.unity");
        exportedPackageAssetList.Add("Assets/Plugins/Android");
        exportedPackageAssetList.Add("Assets/Plugins/IOS");
        exportedPackageAssetList.Add("Assets/Plugins/MacOS");
        exportedPackageAssetList.Add("Assets/Plugins/fpnn");
        exportedPackageAssetList.Add("Assets/Plugins/rtm");
        exportedPackageAssetList.Add("Assets/Plugins/x86");
        exportedPackageAssetList.Add("Assets/Plugins/x86_64");
        exportedPackageAssetList.Add("Assets/Plugins/HomePage.cs");
        exportedPackageAssetList.Add("Assets/Plugins/HomePage.unity");
        exportedPackageAssetList.Add("Assets/Plugins/P2PMode.cs");
        exportedPackageAssetList.Add("Assets/Plugins/P2PMode.unity");
        exportedPackageAssetList.Add("Assets/Plugins/RoomMode.cs");
        exportedPackageAssetList.Add("Assets/Plugins/RoomMode.unity");
        string fileName = "Dist/rtm-sdk-unity-native-"+ RTMConfig.SDKVersion +"-with-rtc.unitypackage";
        AssetDatabase.ExportPackage(exportedPackageAssetList.ToArray(), fileName,
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
    }
}
