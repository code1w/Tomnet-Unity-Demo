
/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/16
 */


/*
 * 网络使用示例
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.NetWork;
using TomNet.Common;
using TomNet.Core;
using TomNet.Protocol;

namespace TomNet
{
    public class Doraemon : IDispatchable
    {
        private EventDispatcher dispatcher;
        private INetWorkClient network;
        private bool inited = false;
        private bool isConnecting = false;
        private string lastHost;

        // 2020-11-03 
        private Queue<BaseEvent> eventsQueue = new Queue<BaseEvent>();
        private bool threadSafeMode = true;
        private object eventsLocker = new object();
        public bool ThreadSafeMode
        {
            get
            {
                return threadSafeMode;
            }
            set
            {
                threadSafeMode = value;
            }
        }


        public EventDispatcher Dispatcher
        {
            get
            {
                return dispatcher;
            }
            set
            {
                dispatcher = value;
            }
        }
        public INetWorkClient NetWork => network;
        public string ConnectionMode => network.ConnectionMode;
        public int CompressionThreshold => network.CompressionThreshold;
        public int MaxMessageSize => network.MaxMessageSize;
        public bool IsConnected
        {
            get
            {
                bool result = false;
                if (network != null)
                {
                    result = network.Connected;
                }
                return result;
            }
        }
        public Doraemon()
        {
            Initialize(false);
        }

        public Doraemon(bool debug)
        {
            Initialize(debug);
        }

        private void Initialize(bool debug)
        {
            if(!inited)
            {
                dispatcher = new EventDispatcher(this);
                network = new TcpNetWorkClinet(new ProtobufProtocol());
                if(network!=null)
                {
                    network.Initialize();
                    RegisterNetWorkEvent();
                }
                inited = true;
            }

        }

        private void RegisterNetWorkEvent()
        {
            network.Dispatcher.AddEventListener(NetWorkEvent.CONNECT, OnNetWorkConnect);
            network.Dispatcher.AddEventListener(NetWorkEvent.CONNECTFAILURE, OnNetWorkConnectFailure);
            network.Dispatcher.AddEventListener(NetWorkEvent.DISCONNECT, OnNetWorkClose);
            network.Dispatcher.AddEventListener(NetWorkEvent.RECONNECTION_TRY, OnNetWorkReconnectionTry);
            network.Dispatcher.AddEventListener(NetWorkEvent.IO_ERROR, OnNetWorkIOError);
            network.Dispatcher.AddEventListener(NetWorkEvent.DATA_ERROR, OnNetWorkDataError);

        }

        public void Connect(string host, int port)
        {
            if (IsConnected)
            {
                return;
            }

            if (isConnecting)
            {
                return;
            }

            if (host == null)
            {
                return;
            }
            if (host == null || host.Length == 0)
            {
                throw new ArgumentException("Invalid connection host name / IP address");
            }
            if (port < 0 || port > 65535)
            {
                throw new ArgumentException("Invalid connection port");
            }
            lastHost = host;
            isConnecting = true;
            network.Connect(host, port);
        }

        private void OnNetWorkConnect(BaseEvent e)
        {
            Console.WriteLine("Doraemon OnNetWorkConnect");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["success"] = true;
            DispatchEvent(new BaseEvent(DoraemonEvent.CONNECTION, dictionary));
        }

        private void OnNetWorkConnectFailure(BaseEvent e)
        {
            Console.WriteLine("Doraemon OnNetWorkConnectFailure");
        }

        private void OnNetWorkClose(BaseEvent e)
        {
            Console.WriteLine("Doraemon OnTcpReconnectionTry");
        }

        private void OnNetWorkReconnectionTry(BaseEvent e)
        {
            Console.WriteLine("Doraemon OnTcpReconnectionTry");
        }

        private void OnNetWorkIOError(BaseEvent e)
        {
            Console.WriteLine("Doraemon OnNetWorkIOError");

        }

        private void OnNetWorkDataError(BaseEvent e)
        {
            Console.WriteLine("Doraemon OnNetWorkDataError");
        }

        public void AddEventListener(string eventType, EventListenerDelegate listener)
        {
            dispatcher.AddEventListener(eventType, listener);
        }


        // 2020-11-03 
        internal void DispatchEvent(BaseEvent evt)
        {
            if (!threadSafeMode)
            {
                Dispatcher.DispatchEvent(evt);
            }
            else
            {
                EnqueueEvent(evt);
            }
        }

        private void EnqueueEvent(BaseEvent evt)
        {
            lock (eventsLocker)
            {
                eventsQueue.Enqueue(evt);
            }
        }

        public void ProcessEvents()
        {
            if (!threadSafeMode)
            {
                return;
            }
            if (eventsQueue.Count != 0)
            {
                BaseEvent[] array;
                lock (eventsLocker)
                {
                    array = eventsQueue.ToArray();
                    eventsQueue.Clear();
                }
                for (int i = 0; i < array.Length; i++)
                {
                    Dispatcher.DispatchEvent(array[i]);
                }
            }
        }
        public void RemoveEventListener(string eventType, EventListenerDelegate listener)
        {
            dispatcher.RemoveEventListener(eventType, listener);
        }

        public void RemoveAllEventListeners()
        {
            dispatcher.RemoveAll();
        }
    }
}
