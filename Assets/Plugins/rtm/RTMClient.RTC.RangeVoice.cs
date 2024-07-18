using System;
using System.Collections.Generic;
using com.fpnn.proto;
using UnityEngine;

namespace com.fpnn.rtm
{
    public partial class RTMClient
    {
        public bool UpdatePosition(DoneDelegate callback, long roomId, int x, int y, int z, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });

                return false;
            }

            Quest quest = new Quest("rangevoice_updatePosition");
            quest.Param("rid", roomId);
            quest.Param("x", x);
            quest.Param("y", y);
            quest.Param("z", z);

            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {
                callback(errorCode);
            }, timeout);


            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });

            return asyncStarted;
        }
        
        public int UpdatePosition(long roomId, int x, int y, int z, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;
        
            Quest quest = new Quest("rangevoice_updatePosition");
            quest.Param("rid", roomId);
            quest.Param("x", x);
            quest.Param("y", y);
            quest.Param("z", z);       
            
            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }
        
        public bool SetVoiceRange(DoneDelegate callback, long roomId, int range, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });
        
                return false;
            }
        
            Quest quest = new Quest("rangevoice_setVoiceRange");
            quest.Param("rid", roomId);
            quest.Param("range", range);
        
            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {
                callback(errorCode);
            }, timeout);
        
        
            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });
        
            return asyncStarted;
        }
        
        public int SetVoiceRange(long roomId, int range, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;
                
            Quest quest = new Quest("rangevoice_setVoiceRange");
            quest.Param("rid", roomId);
            quest.Param("range", range);
                    
            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }
        
        public bool SetMaxReceiveStreams(DoneDelegate callback, long roomId, int maxReceiveStreams, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
            {
                if (RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                    ClientEngine.RunTask(() =>
                    {
                        callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                    });
                
                return false;
            }
                
            Quest quest = new Quest("rangevoice_setMaxReceiveStreams");
            quest.Param("rid", roomId);
            quest.Param("maxReceiveStreams", maxReceiveStreams);
                
            bool asyncStarted = client.SendQuest(quest, (Answer answer, int errorCode) => {
                callback(errorCode);
            }, timeout);
                
                
            if (!asyncStarted && RTMConfig.triggerCallbackIfAsyncMethodReturnFalse)
                ClientEngine.RunTask(() =>
                {
                    callback(fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                });
                
            return asyncStarted;
        }
        
        public int SetMaxReceiveStreams(long roomId, int maxReceiveStreams, int timeout = 0)
        {
            TCPClient client = GetCoreClient();
            if (client == null)
                return fpnn.ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION;
                        
            Quest quest = new Quest("rangevoice_setMaxReceiveStreams");
            quest.Param("rid", roomId);
            quest.Param("maxReceiveStreams", maxReceiveStreams);
                            
            Answer answer = client.SendQuest(quest, timeout);
            return answer.ErrorCode();
        }
    }
}