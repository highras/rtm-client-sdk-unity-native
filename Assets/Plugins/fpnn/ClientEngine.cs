using System;
using System.Collections.Generic;
using System.Threading;

namespace com.fpnn
{
    public static partial class ClientEngine
    {
        private static volatile bool inited;
        private static volatile bool stopped;
        private static object interLocker;
        private static Thread routineThread;
        //private static Semaphore quitSemaphore;
        private static bool forbiddenRegisterConnection;        //-- Unity iOS only.
        private static common.TaskThreadPool taskPool;
        private static bool dropAllTaskWhenQuit;

        internal static DateTime originDateTime;
        internal static int globalConnectTimeoutSeconds;
        internal static int globalQuestTimeoutSeconds;
        internal static int maxPayloadSize;
        internal static common.ErrorRecorder errorRecorder;

        static partial void PlatformInit();     //-- In lock (interLocker) {...}
        static partial void PlatformUninit();

        static ClientEngine()
        {
            inited = false;
            interLocker = new object();
        }

        /*
         * Is NOT necessary, just uniform the interfaces with Unity version.
         */
        public static void Init()
        {
            Init(null);
        }

        /*
         * Customized Init.
         */
        public static void Init(Config config)
        {
            if (inited)
                return;

            lock (interLocker)
            {
                if (inited)
                    return;

                if (config == null)
                    config = new Config();

                //---------------------

                stopped = false;
                forbiddenRegisterConnection = false;
                originDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                dropAllTaskWhenQuit = config.dropAllUnexecutedTaskWhenExiting;
                globalConnectTimeoutSeconds = config.globalConnectTimeoutSeconds;
                globalQuestTimeoutSeconds = config.globalQuestTimeoutSeconds;
                maxPayloadSize = config.maxPayloadSize;
                errorRecorder = config.errorRecorder;

                taskPool = new common.TaskThreadPool(config.taskThreadPoolConfig.initThreadCount,
                    config.taskThreadPoolConfig.perfectThreadCount,
                    config.taskThreadPoolConfig.maxThreadCount,
                    config.taskThreadPoolConfig.maxQueueLengthLimitation,
                    config.taskThreadPoolConfig.tempLatencySeconds,
                    dropAllTaskWhenQuit
                    );

                taskPool.SetErrorRecorder(config.errorRecorder);

                //routineThread = new Thread(RoutineFunc)
                //{
                //    Name = "FPNN.ClientEngine.RoutineThread",
                //    IsBackground = true
                //};
                //routineThread.Start();

                //---------------------

                PlatformInit();

                inited = true;
            }
            ClientManager.Init();
        }

        private static void CheckInitStatus()
        {
            Init(null);
        }

        //private static void RoutineFunc()
        //{
        //    while (!stopped)
        //    {
        //        Thread.Sleep(1000);

        //        Int64 currentSeconds = GetCurrentSeconds();

        //    }

        //    StopAllConnections();
        //    quitSemaphore.Release();
        //}

        /*
         * Only for Unity on iOS devices when apps is going to background.
         */
        internal static void StopAllConnections()
        {
            if (inited == false)
                return;

            CheckInitStatus();

        }

        /*
         * Only for Unity on iOS devices when apps is going to background.
         */
        internal static void ChangeForbiddenRegisterConnection(bool forbidden)
        {
            lock (interLocker)
            {
                forbiddenRegisterConnection = forbidden;
            }
        }

        public static bool RunTask(common.TaskThreadPool.ITask task)
        {
            CheckInitStatus();
            return taskPool.Wakeup(task);
        }

        public static bool RunTask(Action action)
        {
            CheckInitStatus();
            return taskPool.Wakeup(action);
        }

        public static Int64 GetCurrentSeconds()
        {
            TimeSpan span = DateTime.UtcNow - originDateTime;
            return (Int64)Math.Floor(span.TotalSeconds);
        }

        public static Int64 GetCurrentMilliseconds()
        {
            TimeSpan span = DateTime.UtcNow - originDateTime;
            return (Int64)Math.Floor(span.TotalMilliseconds);
        }

        public static Int64 GetCurrentMicroseconds()
        {
            TimeSpan span = DateTime.UtcNow - originDateTime;
            return (Int64)Math.Floor(span.TotalMilliseconds * 1000);
        }

        public static void Close()
        {
            ClientManager.Stop();
            lock (interLocker)
            {
                if (stopped)
                    return;

                stopped = true;
            }

            PlatformUninit();
            taskPool.Close(dropAllTaskWhenQuit);
            Client.closeEngine();
        }
    }
}
