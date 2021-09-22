using System.Threading;
using com.fpnn.proto;
using System;
namespace com.fpnn
{
    internal class AdvancedAnswerInfo
    {
        public Quest quest;
        public Client client;

        private static ThreadLocal<AdvancedAnswerInfo> instance = new ThreadLocal<AdvancedAnswerInfo>(() => { return new AdvancedAnswerInfo(); });

        public static void Reset(Client client, Quest quest)
        {
            AdvancedAnswerInfo ins = instance.Value;
            ins.quest = quest;
            ins.client = client;
        }

        public static Client TakeClient()
        {
            AdvancedAnswerInfo ins = instance.Value;
            Client client = ins.client;
            ins.client = null;
            return client;
        }

        public static AdvancedAnswerInfo Get()
        {
            return instance.Value;
        }

        public static bool Answered()
        {
            AdvancedAnswerInfo ins = instance.Value;
            bool answered = ins.client == null;
            ins.quest = null;
            ins.client = null;
            return answered;
        }
    }

    public static class AdvanceAnswer
    {
        public static bool SendAnswer(Answer answer)
        {
            Client client = AdvancedAnswerInfo.TakeClient();
            if (client != null)
            {
                client.SendAnswer(answer);
                return true;
            }
            else
                return false;
        }
    }
}
