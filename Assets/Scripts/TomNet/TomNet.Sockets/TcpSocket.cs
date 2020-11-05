/*
 * This file is part of the TomNet package.
 *
 * (a)  <qunshuok@gmail.com>
 *
 *  2020/09/10
 */


using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using TomNet.Common;
using TomNet.NetWork;


namespace TomNet.Sockets
{
    public class TcpSocket : BaseSocket, ISocket
    {
        private static readonly int  TcpBufferSize = 8192;
        private static int connid = 0;
        private int socketPollSleep;
        private Thread connthread;
        private string host;
        private int port;
        private TcpClient conn;
        private NetworkStream connstream;
        private Thread readthread;
        private byte[] buffer = new byte[4096];
        private ByteBuffer recvbuf_;

        private OnDataDelegate onData = null;
        private OnErrorDelegate onError = null;
        private ConnectionDelegate onConnect;
        private DisconnectionDelegate onDisconnect;
        private ConnectionFailureDelegate onConnectFailure;
        public bool IsConnected => base.State == States.Connected;
        public OnDataDelegate OnData
        {
            get
            {
                return onData;
            }
            set
            {
                onData = value;
            }
        }

        public OnStringDataDelegate OnStringData
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public OnErrorDelegate OnError
        {
            get
            {
                return onError;
            }
            set
            {
                onError = value;
            }
        }

        public ConnectionDelegate OnConnect
        {
            get
            {
                return onConnect;
            }
            set
            {
                onConnect = value;
            }
        }

        public ConnectionFailureDelegate OnConnectFailure
        {
            get
            {
                return onConnectFailure;
            }
            set
            {
                onConnectFailure = value;
            }
        }

        public DisconnectionDelegate OnDisconnect
        {
            get
            {
                return onDisconnect;
            }
            set
            {
                onDisconnect = value;
            }
        }

        public bool RequiresConnection => true;

        public int SocketPollSleep
        {
            get
            {
                return socketPollSleep;
            }
            set
            {
                socketPollSleep = value;
            }
        }

        
        public TcpSocket(INetWorkClient bs)
        {
            networkclient = bs;
            recvbuf_ = new ByteBuffer();
            InitStates();
        }
        

        public TcpSocket()
        {
            recvbuf_ = new ByteBuffer();
            InitStates();
        }

        private void LogWarn(string msg)
        {
        }


        /// <summary>
        /// �����̸߳�������������
        /// </summary>
        private void ConnectThread()
        {
            Thread.CurrentThread.Name = "ConnectionThread" + connid++;
            try
            {
                conn = new TcpClient(host, port);
                connstream = conn.GetStream();
                ConfigureTcpSocket(conn.Client);
                fsm.ApplyTransition(Transitions.ConnectionSuccess);
                CallOnConnect();
                readthread = new Thread(Read);
                readthread.IsBackground = true;
                readthread.Start();
            }
            catch (SocketException ex)
            {
                string err = "Connection Refused: " + ex.Message + " " + ex.StackTrace;
                HandleConnectFailure(err);
            }
            catch (Exception ex2)
            {
                string err2 = "General exception on connection: " + ex2.Message + " " + ex2.StackTrace;
                HandleConnectFailure(err2);
            }
        }

        private void HandleConnectFailure(string err)
        {
            Hashtable hashtable = new Hashtable();
            hashtable["err"] = err;
            Console.WriteLine(err);
            (networkclient as TcpNetWorkClinet).ThreadManager.EnqueueCustom(HandleConnectFailureCallback, hashtable);
        }

        private void HandleConnectFailureCallback(object state)
        {
            fsm.ApplyTransition(Transitions.ConnectionFailure);
            CallOnConnectFailure();
        }

        private void HandleError(string err)
        {
            HandleError(err, SocketError.NotSocket);
        }

        private void HandleError(string err, SocketError se)
        {
            Hashtable hashtable = new Hashtable();
            hashtable["err"] = err;
            hashtable["se"] = se;
            Console.WriteLine(err);
            Console.WriteLine(se);
            (networkclient as TcpNetWorkClinet).ThreadManager.EnqueueCustom(HandleErrorCallback, hashtable);
        }

        private void HandleErrorCallback(object state)
        {
            Hashtable hashtable = state as Hashtable;
            string msg = (string)hashtable["err"];
            SocketError se = (SocketError)hashtable["se"];
            fsm.ApplyTransition(Transitions.ConnectionFailure);
            if (!isDisconnecting)
            {
                CallOnError(msg, se);
            }
            HandleDisconnection();
        }

        private void HandleDisconnection()
        {
            HandleDisconnection(null);
        }

        private void HandleDisconnection(string reason)
        {
            if (base.State != 0)
            {
                fsm.ApplyTransition(Transitions.Disconnect);
                CallOnDisconnect(reason);
            }
        }

        private void WriteSocket(byte[] data)
        {
            if (base.State != States.Connected)
            {
                return;
            }
            try
            {
                connstream.Write(data, 0, data.Length);
            }
            catch (SocketException ex)
            {
                string err = "Error writing to socket: " + ex.Message;
                HandleError(err, ex.SocketErrorCode);
            }
            catch (Exception ex2)
            {
                string err2 = "General error writing to socket: " + ex2.Message + " " + ex2.StackTrace;
                HandleError(err2);
            }
        }

        private void WriteSocket(ByteBuffer buf)
        {
            if (base.State != States.Connected)
            {
                return;
            }
            try
            {
                connstream.Write(buf.Begin(), buf.PrependableBytes(), buf.Readable());
            }
            catch (SocketException ex)
            {
                string err = "Error writing to socket: " + ex.Message;
                HandleError(err, ex.SocketErrorCode);
            }
            catch (Exception ex2)
            {
                string err2 = "General error writing to socket: " + ex2.Message + " " + ex2.StackTrace;
                HandleError(err2);
            }
        }

        private static void Sleep(int ms)
        {
            Thread.Sleep(10);
        }

        private void Read()
        {
            int num = 0;
            while (true)
            {
                try
                {
                    if (base.State != States.Connected)
                    {
                        return;
                    }
                    if (socketPollSleep > 0)
                    {
                        Sleep(socketPollSleep);
                    }

                    num = connstream.Read(buffer, 0, buffer.Length);
                    if (num < 1)
                    {
                        // ========================
                        // �Զ������Ͽ�����
                        recvbuf_.Reset();
                        HandleError("Connection closed by the remote side");
                        return;
                    }
                    HandleBinaryData(buffer, num);
                }
                catch(SocketException ex)
                {
                    HandleError("General error from socket: " + ex.Message);
                }
                catch (Exception ex)
                {
                    // =============================
                    // �Զ˽���ɱ��
                    HandleError("General error reading data from socket: " + ex.Message + " " + ex.StackTrace);
                    return;
                }
            }
        }

        private void HandleBinaryData(byte[] buf, int size)
        {
            recvbuf_.Write(buf, 0, size);
            CallOnData();
        }

        public void Connect(string host, int port)
        {
            if (base.State != 0)
            {
                LogWarn("Call to Connect method ignored, as the socket is already connected");
                return;
            }
            this.host = host;
            this.port = port;
            fsm.ApplyTransition(Transitions.StartConnect);
            connthread = new Thread(ConnectThread);
            connthread.Start();
        }

        public void Disconnect()
        {
            Disconnect(null);
        }

        public void Disconnect(string reason)
        {
            if (base.State != States.Connected)
            {
                LogWarn("Calling disconnect when the socket is not connected");
                return;
            }
            isDisconnecting = true;
            try
            {
                conn.Client.Shutdown(SocketShutdown.Both);
                conn.Close();
                connstream.Close();
            }
            catch (Exception)
            {
            }
            HandleDisconnection(reason);
            isDisconnecting = false;
        }

        public void Kill()
        {
            fsm.ApplyTransition(Transitions.Disconnect);
            conn.Close();
        }

        private void CallOnData(byte[] data)
        {
            if (onData != null)
            {
            }
        }

        private void CallOnData()
        {
            int readable = recvbuf_.Readable();
            while (readable > sizeof(int))
            {
                int packlen = recvbuf_.PeekInt32(); // ���ﲻ���ö��ķ�ʽ,ֻ��peek
                if (readable < packlen + sizeof(int))
                {
                    break;
                }

                // ====================
                // ��֤�����Ƿ�Ϸ�
                if(packlen > 64*1024)
                {
                    break;
                }

                ByteBuffer data = new ByteBuffer();
                packlen = recvbuf_.ReadInt32();
                data.AppendInt32(packlen);
                data.Write(recvbuf_.Begin(), recvbuf_.PrependableBytes(), packlen);
                recvbuf_.Skip(packlen);
                if (onData != null)
                {
                    (networkclient as TcpNetWorkClinet).ThreadManager.EnqueueDataCall(OnData, data);
                }

                readable = recvbuf_.Readable();
                // ======================
                // ����
                // DebugPacketData(ref data);

            } 

        }

        private void CallOnError(string msg, SocketError se)
        {
            if (onError != null)
            {
                onError(msg, se);
            }
        }

        private void CallOnConnect()
        {
            if (onConnect != null)
            {
                onConnect();
            }
        }

        private void CallOnConnectFailure()
        {
            if(onConnectFailure != null)
            {
                onConnectFailure();
            }
        }

        private void CallOnDisconnect(string reason)
        {
            if (onDisconnect != null)
            {
                onDisconnect(reason);
            }
        }

        public void Write(byte[] data)
        {
            WriteSocket(data);
        }

        public void Write(ByteBuffer data)
        {
            WriteSocket(data);
        }

        private void DebugPacketData(ref ByteBuffer data)
        {
            int len = data.PeekInt32();
            byte[] dbytes = new byte[len+sizeof(int)];
            Buffer.BlockCopy(data.Begin(), data.PrependableBytes(), dbytes, 0, data.Readable());
            string str = System.Text.Encoding.Default.GetString(dbytes);
            Console.WriteLine($"packet len : " + len);
        }

        private void ConfigureTcpSocket(Socket tcpSocket)
        {
            tcpSocket.NoDelay = true;
            tcpSocket.ReceiveBufferSize = TcpBufferSize;
            tcpSocket.SendBufferSize = TcpBufferSize;
        }
    }
}
