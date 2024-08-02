#if UNITY_2017_1_OR_NEWER

using System;
using System.Runtime.InteropServices;

namespace com.fpnn.rtm
{
    internal static class AudioConvert
    {
#if (UNITY_ANDROID || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            [DllImport("audio-convert")]
            public static extern IntPtr convert_wav_to_amrwb(IntPtr wavSrc, int wavSrcSize, ref int status, ref int amrSize);

            [DllImport("audio-convert")]
            public static extern IntPtr convert_amrwb_to_wav(IntPtr amrSrc, int amrSrcSize, ref int status, ref int wavSize);
            
            [DllImport("audio-convert")]
            public static extern void free_memory(IntPtr ptr);


#elif UNITY_IOS

            [DllImport("__Internal")]
            public static extern IntPtr convert_wav_to_amrwb(IntPtr wavSrc, int wavSrcSize, ref int status, ref int amrSize);

            [DllImport("__Internal")]
            public static extern IntPtr convert_amrwb_to_wav(IntPtr amrSrc, int amrSrcSize, ref int status, ref int wavSize);

            [DllImport("__Internal")]
            public static extern void free_memory(IntPtr ptr);

#elif (UNITY_OPENHARMONY)

            [DllImport("__Internal")]
            public static extern IntPtr convert_wav_to_amrwb(IntPtr wavSrc, int wavSrcSize, ref int status, ref int amrSize);

            [DllImport("__Internal")]
            public static extern IntPtr convert_amrwb_to_wav(IntPtr amrSrc, int amrSrcSize, ref int status, ref int wavSize);

            [DllImport("__Internal")]
            public static extern void audio_convert_free_memory(IntPtr ptr);

#endif

        public static byte[] ConvertToAmrwb(byte[] wavBuffer)
        {
            int status = 0;
            int amrSize = 0;
            
            IntPtr wavSrcPtr = Marshal.AllocHGlobal(wavBuffer.Length);
            Marshal.Copy(wavBuffer, 0, wavSrcPtr, wavBuffer.Length);

            IntPtr amrPtr = AudioConvert.convert_wav_to_amrwb(wavSrcPtr, wavBuffer.Length, ref status, ref amrSize);

            Marshal.FreeHGlobal(wavSrcPtr);

            if (amrPtr != null && status == 0) {
                byte[] amrBuffer = new byte[amrSize];
                Marshal.Copy(amrPtr, amrBuffer, 0, amrSize);
                FreeMemory(amrPtr);
                return amrBuffer;
            }

            if (amrPtr != null)
                FreeMemory(amrPtr);

            return null;
        }

        public static byte[] ConvertToWav(byte[] amrBuffer)
        {
            int status = 0;
            int wavSize = 0;

            IntPtr amrSrcPtr = Marshal.AllocHGlobal(amrBuffer.Length);
            Marshal.Copy(amrBuffer, 0, amrSrcPtr, amrBuffer.Length);

            IntPtr wavPtr = AudioConvert.convert_amrwb_to_wav(amrSrcPtr, amrBuffer.Length, ref status, ref wavSize);

            Marshal.FreeHGlobal(amrSrcPtr);

            if (wavPtr != null && status == 0) {
                byte[] wavBuffer = new byte[wavSize];
                Marshal.Copy(wavPtr, wavBuffer, 0, wavSize);
                FreeMemory(wavPtr);
                return wavBuffer;
            }

            if (wavPtr != null)
                FreeMemory(wavPtr);

            return null;
        }

        public static void FreeMemory(IntPtr ptr)
        {
#if (UNITY_OPENHARMONY && !UNITY_EDITOR_WIN && !UNITY_EDITOR_OSX)
            AudioConvert.audio_convert_free_memory(ptr);
#else
            AudioConvert.free_memory(ptr);
#endif
        }
    }
}

#endif