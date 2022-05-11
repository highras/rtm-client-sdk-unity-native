﻿using System;
using System.Collections.Generic;
using com.fpnn.proto;
using UnityEngine;

namespace com.fpnn.rtm
{
    public partial class RTMClient
    {
        //private volatile bool microphone = false;
        //private volatile bool voicePlay = false;
        //private volatile bool camera = false;
        //private volatile bool frontCamera = true;


        //===========================[ Adjust Time ]=========================//
        public bool AdjustTime(Action<long, int> callback, int timeout = 0)
        {
            Client client = GetRTCClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("adjustTime");

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {
                long ts = 0;

                if (errorCode == fpnn.ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        ts = answer.Want<long>("ts");
                    }
                    catch (Exception)
                    {
                        errorCode = fpnn.ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE;
                    }
                }
                callback(ts, errorCode);
            }, timeout);

            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(0, fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }

        public int AdjustTime(out long ts, int timeout = 0)
        {
            ts = 0;
            Client client = GetRTCClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;

            Quest quest = new Quest("adjustTime");

            Answer answer = client.SendQuest(quest, timeout);
            ts = answer.Want<long>("ts");

            return answer.ErrorCode();
        }
    }
}