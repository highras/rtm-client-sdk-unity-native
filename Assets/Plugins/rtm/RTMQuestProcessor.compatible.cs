using System;
using com.fpnn.common;

namespace com.fpnn.rtm
{
    public interface IRTMMasterProcessor: IQuestProcessor
    {
        void SetErrorRecorder(ErrorRecorder recorder);
        void SetConnectionId(UInt64 connId);
        void BeginCheckPingInterval();
        bool ConnectionIsAlive();
        void SessionClosed(int ClosedByErrorCode);

        bool ReloginWillStart(int lastErrorCode, int retriedCount);
        void ReloginCompleted(bool successful, bool retryAgain, int errorCode, int retriedCount);
    }
}
