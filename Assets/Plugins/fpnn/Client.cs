using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using AOT;
using com.fpnn.proto;
using System.Net;

namespace com.fpnn
{
    /*
     * Connection events.
     */
    public delegate void ConnectionConnectedDelegate(UInt64 connectionId, string endpoint, bool connected);
    public delegate void ConnectionCloseDelegate(UInt64 connectionId, string endpoint, bool causedByError);

    public delegate void ConnectionConnectedCallback(IntPtr client, UInt64 connectionId, string endpoint, bool connected);
    public delegate void ConnectionClosedCallback(IntPtr client, UInt64 connectionId, string endpoint, bool causedByError);
    /*
     * Process server pushed quests and return answers if necessary.
     */
    public delegate Answer QuestProcessDelegate(UInt64 connectionId, string endpoint, Quest quest);


    public interface IQuestProcessor
    {
        QuestProcessDelegate GetQuestProcessDelegate(string method);
    }

    public class AnswerCallbackUnit
    {
        public IAnswerCallback callback;
        public UInt32 seqNum;
        public Int64 timeoutTime;
    }

    abstract public class Client
    {
        public delegate void LoggerCallBack(IntPtr log, UInt32 len);
        [MonoPInvokeCallback(typeof(LoggerCallBack))]
        public static void Log(IntPtr log, UInt32 len)
        {
            string payload = Marshal.PtrToStringAnsi(log);
            Debug.Log(payload);
        }

#if UNITY_IOS

        [DllImport("__Internal")]
        protected static extern IntPtr createTCPClient(string host, int port, bool autoConnect);

        [DllImport("__Internal")]
        protected static extern IntPtr createUDPClient(string host, int port, bool autoConnect);

        [DllImport("__Internal")]
        protected static extern IntPtr createTCPClientWithEndpoint(string endpoint, bool autoConnect);

        [DllImport("__Internal")]
        protected static extern IntPtr createUDPClientWithEndpoint(string endpoint, bool autoConnect);

        [DllImport("__Internal")]
        protected static extern void destroyClient(IntPtr clientDelegate);

        [DllImport("__Internal")]
        protected static extern UInt64 connectionId(IntPtr clientDelegate);

        [DllImport("__Internal")]
        protected static extern bool isConnected(IntPtr clientDelegate);

        [DllImport("__Internal")]
        protected static extern bool syncConnect(IntPtr clientDelegate);

        [DllImport("__Internal")]
        protected static extern bool asyncConnect(IntPtr clientDelegate);

        [DllImport("__Internal")]
        protected static extern void closeClient(IntPtr clientDelegate);

        [DllImport("__Internal")]
        protected static extern bool sendData(IntPtr clientDelegate, byte[] data, int len);

        [DllImport("__Internal")]
        protected static extern void registerConnectedCallback(IntPtr processorDelegate, ConnectionConnectedCallback callback);

        [DllImport("__Internal")]
        protected static extern void registerClosedCallback(IntPtr processorDelegate, ConnectionClosedCallback callback);

        [DllImport("__Internal")]
        protected static extern void setQuestProcessor(IntPtr client, IntPtr clientDelegate, IntPtr questProcessorDelegate);

        [DllImport("__Internal")]
        protected static extern IntPtr createQuestProcessor();

        [DllImport("__Internal")]
        protected static extern void destroyQuestProcessor(IntPtr processorDelegate);

        [DllImport("__Internal")]
        public static extern bool closeEngine();

        [DllImport("__Internal")]
        public static extern void SetLogger(LoggerCallBack callback);

#else
        [DllImport("fpnn")]
        protected static extern IntPtr createTCPClient(string host, int port, bool autoConnect);

        [DllImport("fpnn")]
        protected static extern IntPtr createUDPClient(string host, int port, bool autoConnect);

        [DllImport("fpnn")]
        private static extern IntPtr createTCPClientWithEndpoint(string endpoint, bool autoConnect);

        [DllImport("fpnn")]
        protected static extern IntPtr createUDPClientWithEndpoint(string endpoint, bool autoConnect);

        [DllImport("fpnn")]
        protected static extern void destroyClient(IntPtr clientDelegate);

        [DllImport("fpnn")]
        protected static extern UInt64 connectionId(IntPtr clientDelegate);

        [DllImport("fpnn")]
        protected static extern bool isConnected(IntPtr clientDelegate);

        [DllImport("fpnn")]
        protected static extern bool syncConnect(IntPtr clientDelegate);

        [DllImport("fpnn")]
        protected static extern bool asyncConnect(IntPtr clientDelegate);

        [DllImport("fpnn")]
        protected static extern void closeClient(IntPtr clientDelegate);

        [DllImport("fpnn")]
        protected static extern bool sendData(IntPtr clientDelegate, byte[] data, int len);

        [DllImport("fpnn")]
        protected static extern void registerConnectedCallback(IntPtr processorDelegate, ConnectionConnectedCallback callback);

        [DllImport("fpnn")]
        protected static extern void registerClosedCallback(IntPtr processorDelegate, ConnectionClosedCallback callback);

        [DllImport("fpnn")]
        protected static extern void setQuestProcessor(IntPtr client, IntPtr clientDelegate, IntPtr questProcessorDelegate);

        [DllImport("fpnn")]
        protected static extern IntPtr createQuestProcessor();

        [DllImport("fpnn")]
        protected static extern void destroyQuestProcessor(IntPtr processorDelegate);

        [DllImport("fpnn")]
        public static extern bool closeEngine();

        [DllImport("fpnn")]
        public static extern void SetLogger(LoggerCallBack callback);

#endif


        protected static IntPtr CreateTCPClient(string host, int port, bool autoConnect)
        {
            return createTCPClient(host, port, autoConnect);
        }

        protected static IntPtr CreateUDPClient(string host, int port, bool autoConnect)
        {
            return createUDPClient(host, port, autoConnect);
        }

        //----------------[ fields ]-----------------------//
        protected IQuestProcessor questProcessor;
        protected common.ErrorRecorder errorRecorder;
        protected ConnectionConnectedDelegate connectConnectedDelegate;
        protected ConnectionCloseDelegate connectionCloseDelegate;
        protected IntPtr clientDelegate;
        protected IntPtr processorDelegate;
        //protected IntPtr thisClient;
        protected GCHandle gcClient;
        protected readonly DnsEndPoint dnsEndPoint;
        protected object interLocker;
        protected Dictionary<UInt32, AnswerCallbackUnit> callbackSeqNumMap;


        //----------------[ Constructor ]-----------------------//
        public Client(string host, int port, bool autoConnect = true)
        {
            SetLogger(Log);
            interLocker = new object();
            callbackSeqNumMap = new Dictionary<UInt32, AnswerCallbackUnit>();
            dnsEndPoint = new DnsEndPoint(host, port);
            errorRecorder = ClientEngine.errorRecorder;
            questProcessor = null;
            processorDelegate = IntPtr.Zero;
        }

        ~Client()
        {
            gcClient.Free();
            destroyClient(clientDelegate);
            destroyQuestProcessor(processorDelegate);
        }

        //----------------[ Properties methods ]-----------------------//

        public string Endpoint()
        {
            return dnsEndPoint.ToString();
        }

        public bool IsConnected()
        {
            return isConnected(clientDelegate);
        }

        //----------------[ Connect Operations ]-----------------------//
        public void AsyncConnect()
        {
            lock (interLocker)
            {
                asyncConnect(clientDelegate);
            }
        }

        public bool SyncConnect()
        {
            bool res;
            lock (interLocker)
            {
                res = syncConnect(clientDelegate);
            }
            return res;
        }

        public void AsyncReconnect()
        {
            Close();
            AsyncConnect();
        }

        public bool SyncReconnect()
        {
            Close();
            return SyncConnect();
        }

        public void Close()
        {
            lock (interLocker)
            {
                closeClient(clientDelegate);
                ClearCallbackMap();
            }
        }

        //----------------[ Operations ]-----------------------//
        public bool SendQuest(Quest quest, AnswerDelegate callback, int timeout = 0)
        {
            byte[] raw;
            try
            {
                raw = quest.Raw();
            }
            catch (Exception ex)
            {
                if (errorRecorder != null)
                    errorRecorder.RecordError("SendQuest: " + quest.Method(), ex);

                return false;
            }

            if (quest.IsOneWay())
            {
                return sendData(clientDelegate, raw, raw.Length);
            }

            if (timeout == 0)
                timeout = ClientEngine.globalQuestTimeoutSeconds;

            AnswerCallbackUnit unit = new AnswerCallbackUnit();
            AnswerDelegateCallback cb = new AnswerDelegateCallback(callback);
            unit.callback = cb;
            unit.seqNum = quest.SeqNum();
            unit.timeoutTime = ClientEngine.GetCurrentSeconds() + timeout;
            lock (interLocker)
            {
                callbackSeqNumMap.Add(unit.seqNum, unit);
            }
            bool ret = sendData(clientDelegate, raw, raw.Length);
            if (!ret)
            {
                lock (interLocker)
                {
                    callbackSeqNumMap.Remove(unit.seqNum);
                }
                return false;
            }
            return true;
        }

        public Answer SendQuest(Quest quest, int timeout = 0)
        {
            byte[] raw;
            try
            {
                raw = quest.Raw();
            }
            catch (Exception ex)
            {
                if (errorRecorder != null)
                    errorRecorder.RecordError("SendQuest: " + quest.Method(), ex);
                return null;
            }

            if (quest.IsOneWay())
            {
                sendData(clientDelegate, raw, raw.Length);
                return null;
            }

            if (timeout == 0)
                timeout = ClientEngine.globalQuestTimeoutSeconds;

            SyncAnswerCallback syncCallBack = new SyncAnswerCallback(quest);
            AnswerCallbackUnit unit = new AnswerCallbackUnit();
            unit.callback = syncCallBack;
            unit.seqNum = quest.SeqNum();
            unit.timeoutTime = ClientEngine.GetCurrentSeconds() + timeout;
            lock (interLocker)
            {
                callbackSeqNumMap.Add(unit.seqNum, unit);
            }
            bool ret = sendData(clientDelegate, raw, raw.Length);
            if (!ret)
            {
                lock (interLocker)
                {
                    callbackSeqNumMap.Remove(unit.seqNum);
                }
                Answer answer = new Answer(quest);
                answer.FillErrorCode(ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION);
                return answer;
            }
            return syncCallBack.GetAnswer();
        }


        public void Connected(UInt64 connectionId, string endpoint, bool connected)
        {
            lock (interLocker)
            {
                if (connectConnectedDelegate != null)
                    connectConnectedDelegate(connectionId, endpoint, connected);
                if (connected == false)
                    ClearCallbackMap();
            }
        }

        public void ClearCallbackMap()
        {
            Dictionary<UInt32, AnswerCallbackUnit> oldCallbackSeqNumMap = new Dictionary<uint, AnswerCallbackUnit>();
            lock (interLocker)
            {
                Dictionary<UInt32, AnswerCallbackUnit> tmp = callbackSeqNumMap;
                callbackSeqNumMap = oldCallbackSeqNumMap;
                oldCallbackSeqNumMap = tmp;
                callbackSeqNumMap.Clear();
            }
            foreach (KeyValuePair<UInt32, AnswerCallbackUnit> kv in oldCallbackSeqNumMap)
            {
                ClientEngine.RunTask(() =>
                {
                    kv.Value.callback.OnException(null, ErrorCode.FPNN_EC_CORE_CONNECTION_CLOSED);
                });
            }
        }

        public void Closed(UInt64 connectionId, string endpoint, bool causedByError)
        {
            lock (interLocker)
            {
                if (connectionCloseDelegate != null)
                    connectionCloseDelegate(connectionId, endpoint, causedByError);
                ClearCallbackMap();
            }
        }

        //----------------[ Configure Operations ]-----------------------//
        public void SetConnectionConnectedDelegate(ConnectionConnectedDelegate ccd)
        {
            lock (interLocker)
            {
                connectConnectedDelegate = ccd;
            }
        }

        public void SetConnectionCloseDelegate(ConnectionCloseDelegate cwcd)
        {
            lock (interLocker)
            {
                connectionCloseDelegate = cwcd;
            }
        }

        public void SetErrorRecorder(common.ErrorRecorder recorder)
        {
            lock (interLocker)
            {
                errorRecorder = recorder;
            }
        }

        public void SetQuestProcessor(IQuestProcessor processor)
        {
            lock (interLocker)
            {
                questProcessor = processor;
                if (processorDelegate != IntPtr.Zero)
                    destroyQuestProcessor(processorDelegate);
                processorDelegate = createQuestProcessor();
                setQuestProcessor(clientDelegate, GCHandle.ToIntPtr(gcClient), processorDelegate);
            }
        }

        public void TakeExpiredAnswerCallback(ref List<AnswerCallbackUnit> expiredList)
        {
            Int64 seconds = ClientEngine.GetCurrentSeconds();
            List<UInt32> expiredSeqList = new List<UInt32>();
            lock (interLocker)
            {
                foreach (KeyValuePair<UInt32, AnswerCallbackUnit> kv in callbackSeqNumMap)
                {
                    if (kv.Value.timeoutTime < seconds)
                    {
                        expiredList.Add(kv.Value);
                        expiredSeqList.Add(kv.Key);
                    }
                }
                foreach (UInt32 key in expiredSeqList)
                {
                    callbackSeqNumMap.Remove(key);
                }
            }
        }

        public QuestProcessDelegate GetQuestProcessDelegate(string method)
        {
            if (questProcessor == null)
                return null;
            QuestProcessDelegate process = questProcessor.GetQuestProcessDelegate(method);
            if (process == null)
                return null;
            return process;
        }

        public void ProcessQuest(UInt64 connectionId, Quest quest)
        {
            if (questProcessor == null)
            {
                if (quest.IsTwoWay())
                {
                    Answer answer = new Answer(quest);
                    answer.FillErrorInfo(ErrorCode.FPNN_EC_CORE_INVALID_PACKAGE, "Client without quest processor.");
                    SendAnswer(answer);
                }
                return;
            }

            ClientEngine.RunTask(() =>
            {
                QuestProcessDelegate process = questProcessor.GetQuestProcessDelegate(quest.Method());
                Answer answer = null;
                if (process == null)
                {
                    if (quest.IsTwoWay())
                    {
                        answer = new Answer(quest);
                        answer.FillErrorInfo(ErrorCode.FPNN_EC_CORE_UNKNOWN_METHOD, "This method is not supported by client.");
                        SendAnswer(answer);
                    }
                    return;
                }
                bool asyncAnswered = false;
                string endpoint = Endpoint();
                AdvancedAnswerInfo.Reset(this, quest);

                try
                {
                    answer = process(connectionId, endpoint, quest);
                }
                catch (Exception ex)
                {
                    if (errorRecorder != null)
                        errorRecorder.RecordError("Run quest process for method: " + quest.Method(), ex);
                }
                finally
                {
                    asyncAnswered = AdvancedAnswerInfo.Answered();
                }

                if (quest.IsTwoWay() && !asyncAnswered)
                {
                    if (answer == null)
                    {
                        answer = new Answer(quest);
                        answer.FillErrorInfo(ErrorCode.FPNN_EC_CORE_UNKNOWN_ERROR, "Two way quest " + quest.Method() + " lose an answer.");
                    }
                    SendAnswer(answer);
                }
                else
                {
                    if (answer != null && errorRecorder != null)
                    {
                        if (quest.IsOneWay())
                            errorRecorder.RecordError("Answer created for one way quest: " + quest.Method());
                        else
                            errorRecorder.RecordError("Answer created reduplicated for two way quest: " + quest.Method());
                    }
                }
            });
        }

        public void ProcessAnswer(Answer answer)
        {
            bool find = false;
            UInt32 seqNum = answer.SeqNum();
            AnswerCallbackUnit unit = null;
            lock (interLocker)
            {
                if (callbackSeqNumMap.TryGetValue(seqNum, out unit))
                {
                    find = true;
                    callbackSeqNumMap.Remove(seqNum);
                }
            }
            if (find)
            {
                ClientEngine.RunTask(() => {
                    int errorCode = answer.ErrorCode();
                    if (errorCode == ErrorCode.FPNN_EC_OK)
                    {
                        unit.callback.OnAnswer(answer);
                    }
                    else
                        unit.callback.OnException(null, errorCode);
                });
            }
        }

        public void SendAnswer(Answer answer)
        {
            byte[] raw = answer.Raw();
            lock (interLocker)
            {
                sendData(clientDelegate, raw, raw.Length);
            }
        }
    }
}

