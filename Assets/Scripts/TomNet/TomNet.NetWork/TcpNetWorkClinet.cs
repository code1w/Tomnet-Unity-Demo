/* 
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/15
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Sockets;
using Google.Protobuf;

using TomNet.Common;
using TomNet.Core;
using TomNet.Sockets;
using TomNet.Protocol;
using TomNet.Controller;

namespace TomNet.NetWork
{
    public class TcpNetWorkClinet : INetWorkClient, IDispatchable
    {
        private readonly double reconnectionDelayMillis = 1000.0;
        private  int reconnectionSeconds = 30;  // 断线重连最大尝试时间 秒
        private ISocket socket = null;
        private IIOHandler ioHandler = null;
        private IProtocol protocol = null;
        private IController controller = null;

        private string connectionMode;
        private ThreadManager threadManager = new ThreadManager();
        private int compressionThreshold = 2000000;  // 压缩阈值
        private int maxMessageSize = 1024*64;
        private int reconnCounter = 0;
        private DateTime firstReconnAttempt = DateTime.MinValue;
        private bool attemptingReconnection = false;
        private string lastHost = "";
        private int lastTcpPort = 0;
        private bool controllersInited = false;
        private EventDispatcher dispatcher = null;
        private System.Timers.Timer retryTimer = null;


        public string ConnectionMode => connectionMode;
        public ThreadManager ThreadManager => threadManager;
        public bool IsBinProtocol => true;

        //       public SystemController SysController => sysController;

        //        public ExtensionController ExtController => extController;

        public ISocket Socket => socket;

        public IIOHandler IoHandler
        {
            get
            {
                if(ioHandler!=null)
                {
                    return ioHandler;
                }
                else
                {
                    return null;
                }

            }
            set
            {
                if (ioHandler == null)
                {
                    ioHandler = value;
                }
            }
        }

        public IProtocol Protocol
        {
            get
            {
                if(protocol != null)
                {
                    return protocol;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if(protocol == null)
                {
                    protocol = value;
                }
            }
        }

        public IController Controller
        {
            get
            {
                if(controller != null)
                {
                    return controller;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if(controller == null)
                {
                    controller = value;
                }
            }
        }

        public bool Connected
        {
            get
            {
                if (socket == null)
                {
                    return false;
                }
                return socket.IsConnected;
            }
        }

        public int CompressionThreshold
        {
            get
            {
                return compressionThreshold;
            }
            set
            {
                if (value > 100)
                {
                    compressionThreshold = value;
                    return;
                }
                throw new ArgumentException("Compression threshold cannot be < 100 bytes");
            }
        }

        public int MaxMessageSize
        {
            get
            {
                return maxMessageSize;
            }
            set
            {
                maxMessageSize = value;
            }
        }

        public bool IsReconnecting
        {
            get
            {
                return attemptingReconnection;
            }
            set
            {
                attemptingReconnection = value;
            }
        }

        public int ReconnectionSeconds
        {
            get
            {
                return reconnectionSeconds;
            }
            set
            {
                if (value < 0)
                {
                    reconnectionSeconds = 0;
                }
                else
                {
                    reconnectionSeconds = value;
                }
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

        public string ConnectionHost
        {
            get
            {
                if (!Connected)
                {
                    return "Not Connected";
                }
                return lastHost;
            }
        }

        public int ConnectionPort
        {
            get
            {
                if (!Connected)
                {
                    return -1;
                }
                return lastTcpPort;
            }
        }

        public TcpNetWorkClinet(IProtocol protocol)
        {
            if(protocol == null)
            {
                this.protocol = new ProtobufProtocol();
            }
            else
            {
                this.protocol = protocol;
            }

        }

        public void Initialize()
        {
            if(dispatcher == null)
            {
                dispatcher = new EventDispatcher(this);
            }
            if (!controllersInited)
            {
                IoHandler = new NetWorkIOHandler(this);
                controllersInited = true;
            }

            if(socket == null)
            {
                socket = new TcpSocket(this);
                if(socket != null)
                {
                    RegisterSocketEventCb();
                }
            }

        }

        private void RegisterSocketEventCb()
        {
            socket.OnConnect = (ConnectionDelegate)Delegate.Combine(socket.OnConnect, new ConnectionDelegate(OnSocketConnect));
            socket.OnConnectFailure = (ConnectionFailureDelegate)Delegate.Combine(socket.OnConnectFailure, new ConnectionFailureDelegate(OnSocketConnectFailure));
            socket.OnDisconnect = (DisconnectionDelegate)Delegate.Combine(socket.OnDisconnect, new DisconnectionDelegate(OnSocketClose));
            socket.OnError = (OnErrorDelegate)Delegate.Combine(socket.OnError, new OnErrorDelegate(OnSocketError));
            socket.OnData = (OnDataDelegate)Delegate.Combine(socket.OnData, new OnDataDelegate(OnSocketData));
        }

        private void OnSocketConnect()
        {
            attemptingReconnection = false;
            NetWorkEvent networkEvent = new NetWorkEvent(NetWorkEvent.CONNECT);
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["success"] = true;
            dictionary["isReconnection"] = attemptingReconnection;
            networkEvent.Params = dictionary;
            DispatchEvent(networkEvent);
        }

        private void OnSocketConnectFailure()
        {
            if(!attemptingReconnection)
            {
                firstReconnAttempt = DateTime.Now;
            }
            attemptingReconnection = true;

            NetWorkEvent networkEvent = new NetWorkEvent(NetWorkEvent.CONNECTFAILURE);
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["success"] = false;
            dictionary["isReconnection"] = attemptingReconnection;
            networkEvent.Params = dictionary;
            DispatchEvent(networkEvent);

            Reconnect();
        }

        private void OnSocketClose(string reason = null)
        {
            if (attemptingReconnection)
            {
                Reconnect();
                return;
            }

            attemptingReconnection = true;
            firstReconnAttempt = DateTime.Now;
            reconnCounter = 1;
            DispatchEvent(new NetWorkEvent(NetWorkEvent.RECONNECTION_TRY));
            Reconnect();

        }

        private void OnSocketError(string message, SocketError se)
        {
            NetWorkEvent bitSwarmEvent = new NetWorkEvent(NetWorkEvent.IO_ERROR);
            bitSwarmEvent.Params = new Dictionary<string, object>();
            bitSwarmEvent.Params["message"] = message + " ==> " + se;
            DispatchEvent(bitSwarmEvent);
        }

        private void OnSocketData(ByteBuffer data)
        {
            try
            {
                ioHandler.OnDataRead(data);
            }
            catch (Exception ex)
            {
                NetWorkEvent bitSwarmEvent = new NetWorkEvent(NetWorkEvent.DATA_ERROR);
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary["message"] = ex.ToString();
                bitSwarmEvent.Params = dictionary;
                DispatchEvent(bitSwarmEvent);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void Reconnect()
        {
            if (attemptingReconnection)
            {
                DateTime now = DateTime.Now;
                TimeSpan t = firstReconnAttempt + new TimeSpan(0, 0, reconnectionSeconds) - now;
                if (t > TimeSpan.Zero)
                {
                    Console.WriteLine("Reconnection attempt: " + reconnCounter + " - time left:" + t.TotalSeconds + " sec.");
                    SetTimeout(OnRetryConnectionEvent, reconnectionDelayMillis);
                    reconnCounter++;
                }
                else
                {
                    Console.WriteLine("Exit Reconnect");
                    ExecuteDisconnection();
                }
            }
        }

        private void SetTimeout(ElapsedEventHandler handler, double timeout)
        {
            
            if (retryTimer == null)
            {
                retryTimer = new System.Timers.Timer(timeout);
                retryTimer.Elapsed += handler;
            }
            retryTimer.AutoReset = false;
            retryTimer.Enabled = true;
            retryTimer.Start();
        }

        private void OnRetryConnectionEvent(object source, ElapsedEventArgs e)
        {
            retryTimer.Enabled = false;
            retryTimer.Stop();
            socket.Connect(lastHost, lastTcpPort);
        }

        private void ExecuteDisconnection(string reason = null)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["reason"] = ((reason == null) ? DisconnectionReason.UNKNOWN : reason);
            DispatchEvent(new NetWorkEvent(NetWorkEvent.DISCONNECT, dictionary));
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            threadManager.Stop();
        }


        private void DispatchEvent(NetWorkEvent evt)
        {
            dispatcher.DispatchEvent(evt);
        }
        public void Destroy()
        {

        }

        public void Connect(string host, int port)
        {
            lastHost = host;
            lastTcpPort = port;
            threadManager.Start();
            socket.Connect(lastHost, lastTcpPort);
            connectionMode = ConnectionModes.SOCKET;
        }


        public void RegisterMsgCallback(string type, RequestProtoBufDelegate cb)
        {
            Controller.RegisterMsgCallback(type, cb);
        }

        public void RegisterMsgType(Type type)
        {
            Protocol.Codec.RegisterMsgType(type.FullName, type);
        }

        public void RegisterMsgCallback(Type type, RequestProtoBufDelegate cb)
        {
            RegisterMsgType(type);
            RegisterMsgCallback(type.FullName, cb);
        }


        public void Send(byte[] data)
        {

        }

        public void Send(object message)
        {
            try
            {
                ByteBuffer data = this.Protocol.Codec.GenerateBinaryMessage(message);
                ioHandler.OnDataWrite(data); 
            }
            catch(Exception ex)
            {

            }

        }

        public void Disconnect()
        {

        }

        public void Disconnect(string reason)
        {

        }

        public void StopReconnection()
        {

        }


        public void KillConnection()
        {

        }

        public void AddEventListener(string eventType, EventListenerDelegate listener)
        {
            dispatcher.AddEventListener(eventType, listener);
        }
    }
}
