using com.fpnn.rtm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class VideoSurface : MonoBehaviour
{
    private RawImage rawImage;
    private Transform transform;
    private Texture2D nativeTexture;
    private TextureFormat textureFormat = TextureFormat.BGRA32;
    private int width = 320;
    private int height = 240;
    private bool localVideo = true;
    private long uid = 0;
    private bool facing = false;
    private bool init = false;
    private IntPtr data = Marshal.AllocHGlobal(1920 * 1080 * 4);
    float scaleX = 1.0f;
    float scaleY = 1.0f;
    float scaleZ = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_IOS
        textureFormat = TextureFormat.BGRA32;
#elif UNITY_ANDROID
        textureFormat = TextureFormat.ARGB32;
#endif
        nativeTexture = new Texture2D(width, height, textureFormat, false);
        rawImage = GetComponent<RawImage>();
        transform = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (uid == 0)
            return;

        int size = 0;

        bool facing = false;
        RTCEngine.GetVideoFrame(uid, data, ref size, ref facing);
        if (size <= 0)
            return;
        if (init == false)
        {
            init = true;
            this.facing = facing;
            ChangeRotation();
        }

        if (this.facing != facing)
        {
            this.facing = facing;
            ChangeRotation();
        }

        nativeTexture.LoadRawTextureData(data, width * height * 4);
        rawImage.texture = nativeTexture;
        nativeTexture.Apply();
    }

    void ChangeRotation()
    {
        transform.localEulerAngles = new Vector3(0.0f, 0.0f, 90.0f);
        if (facing)
        {
            transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }
        else
        {
            transform.localScale = new Vector3(-scaleX, scaleY, scaleZ);
        }
    }

    void OnDestroy()
    {
        if (data != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(data);
            data = IntPtr.Zero;
        }

        if (nativeTexture != null)
        {
            Destroy(nativeTexture);
            nativeTexture = null;
        }
    }

    public void SetVideoInfo(long uid, bool localVideo)
    {
        this.uid = uid;
        this.localVideo = localVideo;
    }

    public void ClearVideoInfo()
    {
        uid = 0;
        localVideo = false;
        init = false;
    }

    public long Uid()
    {
        return uid;
    }

    public void SetScale(float scaleX, float scaleY, float scaleZ)
    {
        this.scaleX = scaleX;
        this.scaleY = scaleY;
        this.scaleZ = scaleZ;
    }
}
