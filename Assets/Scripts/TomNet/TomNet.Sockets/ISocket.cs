/*
 * This file is part of the TomNet package.
 *
 * (a)  <qunshuok@gmail.com>
 *
 *  2020/09/10
 */
using TomNet.Common;

namespace TomNet.Sockets
{
    public interface ISocket
    {
        bool IsConnected
        {
            get;
        }

        bool RequiresConnection
        {
            get;
        }

        ConnectionDelegate OnConnect
        {
            get;
            set;
        }

        ConnectionFailureDelegate OnConnectFailure
        {
            get;
            set;
        }

        DisconnectionDelegate OnDisconnect
        {
            get;
            set;
        }

        OnDataDelegate OnData
        {
            get;
            set;
        }

        OnStringDataDelegate OnStringData
        {
            get;
            set;
        }

        OnErrorDelegate OnError
        {
            get;
            set;
        }
        void Connect(string host, int port);
        void Disconnect();
        void Disconnect(string reason);
        void Write(byte[] data);
        void Write(ByteBuffer data);
        void Kill();
    }
}
