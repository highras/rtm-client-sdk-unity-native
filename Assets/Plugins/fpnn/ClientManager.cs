using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using AOT;
using com.fpnn.proto;

namespace com.fpnn
{
    public class ClientManager
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void RecvDataDelegate(UInt64 connectionId, IntPtr buffer, int length);

        [MonoPInvokeCallback(typeof(RecvDataDelegate))]
        static void RecvDataCallBack(UInt64 connectionId, IntPtr buffer, int length)
        {
            if (length < Message.FPNNHeaderLength)
                return;
            byte[] payload = new byte[length];
            Marshal.Copy(buffer, payload, 0, length);
            if (payload[6] == 0 || payload[6] == 1)
            {//Quest
                Quest quest = new Quest(payload);
                ClientManager.dealQuest(connectionId, quest);
            }
            else if (payload[6] == 2)
            {//Answer
                Answer answer = new Answer(payload);
                ClientManager.dealAnswer(connectionId, answer);
            }
        }
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void setRecvDataCallback(RecvDataDelegate callback);
#else
        [DllImport("fpnn")]
        private static extern void setRecvDataCallback(RecvDataDelegate callback);
#endif

        private static object interLocker;
        private static Dictionary<UInt64, Client> clientMap;
        private static volatile bool running;
        private static Thread routineThread;
        static ClientManager()
        {
        }

        public static void Init()
        { 
            interLocker = new object();
            clientMap = new Dictionary<UInt64, Client>();
            setRecvDataCallback(RecvDataCallBack);
            running = true;
            routineThread = new Thread(routine);
            routineThread.Start();
            //Application.quitting += () => {
            //    Stop();
            //};
        }

        public static void Stop()
        {
            lock (interLocker)
            {
                if (running == false)
                    return;
                running = false;

                foreach (KeyValuePair<UInt64, Client> kv in clientMap)
                    kv.Value.Close();
                clientMap.Clear();
            }
            routineThread.Join();
        }

        public static void registerClient(UInt64 connectionId, Client client)
        {
            lock (interLocker)
            {
                clientMap.Add(connectionId, client);
            }
        }

        public static void unregisterClient(UInt64 connectionId)
        {
            lock (interLocker)
            {
                clientMap.Remove(connectionId);
            }
        }

        public static Client getClient(UInt64 connectionId)
        {
            lock (interLocker)
            {
                if (clientMap.TryGetValue(connectionId, out Client client))
                {
                    return client;
                }
                else
                {
                    return null;
                }
            }
        }

        public static void dealQuest(UInt64 connectionId, Quest quest)
        {
            Client client = getClient(connectionId);
            if (client == null)
                return;
            client.ProcessQuest(connectionId, quest);
        }

        public static void dealAnswer(UInt64 connectionId, Answer answer)
        {
            Client client = getClient(connectionId);
            if (client == null)
                return;
            client.ProcessAnswer(answer);
        }

        private static void routine()
        {
            List<AnswerCallbackUnit> expiredCallbackList = new List<AnswerCallbackUnit>();
            while (running)
            {
                Thread.Sleep(1000);
                lock (interLocker)
                {
                    foreach (KeyValuePair<UInt64, Client> kv in clientMap)
                    {
                        kv.Value.TakeExpiredAnswerCallback(ref expiredCallbackList);
                    }
                }
                foreach (AnswerCallbackUnit cb in expiredCallbackList)
                {
                    ClientEngine.RunTask(() =>
                    {
                        cb.callback.OnException(null, ErrorCode.FPNN_EC_CORE_TIMEOUT);
                    });
                }
                expiredCallbackList.Clear();
            }
            lock (interLocker)
            {
                foreach (KeyValuePair<UInt64, Client> kv in clientMap)
                {
                    kv.Value.Close();
                }
            }
        }
    }
}

