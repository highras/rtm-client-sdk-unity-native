using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using AOT;
using com.fpnn.proto;
using System.Net;

namespace com.fpnn
{
    public class UDPClient : Client
    {
        [MonoPInvokeCallback(typeof(ConnectionConnectedCallback))]
        public static void ConnectedCallback(IntPtr client, UInt64 connectionId, string endpoint, bool connected)
        {
            GCHandle gcHandler = GCHandle.FromIntPtr(client);
            UDPClient clientInterface = (UDPClient)gcHandler.Target;
            ClientManager.registerClient(connectionId, clientInterface);
            clientInterface.Connected(connectionId, endpoint, connected);
        }

        [MonoPInvokeCallback(typeof(ConnectionClosedCallback))]
        public static void ClosedCallback(IntPtr client, UInt64 connectionId, string endpoint, bool causedByError)
        {
            GCHandle gcHandler = GCHandle.FromIntPtr(client);
            UDPClient clientInterface = (UDPClient)gcHandler.Target;
            clientInterface.Closed(connectionId, endpoint, causedByError);
            ClientManager.unregisterClient(connectionId);
        }


        //----------------[ Constructor ]-----------------------//
        public UDPClient(string host, int port, bool autoConnect = true) : base(host, port, autoConnect)
        {
            clientDelegate = CreateUDPClient(dnsEndPoint.Host, dnsEndPoint.Port, autoConnect);
            gcClient = GCHandle.Alloc(this);
        }

        ~UDPClient()
        {
        }

        public static UDPClient Create(string host, int port, bool autoConnect = true)
        {
            return new UDPClient(host, port, autoConnect);
        }

        public static UDPClient Create(string endpoint, bool autoConnect = true)
        {
            int idx = endpoint.LastIndexOf(':');
            if (idx == -1)
                throw new ArgumentException("Invalid endpoint: " + endpoint);

            string host = endpoint.Substring(0, idx);
            string portString = endpoint.Substring(idx + 1);
            int port = Convert.ToInt32(portString, 10);

            return new UDPClient(host, port, autoConnect);
        }

        new public void SetQuestProcessor(IQuestProcessor processor)
        {
            base.SetQuestProcessor(processor);
            lock (interLocker)
            {
                registerConnectedCallback(processorDelegate, ConnectedCallback);
                registerClosedCallback(processorDelegate, ClosedCallback);
            }
        }
    }
}

