using Tom.Bitswarm.BBox;
using Tom.Controllers;
using Tom.Core;
using Tom.Core.Sockets;
using Tom.Logging;
using Tom.Util;

namespace Tom.Bitswarm
{
	public interface ISocketClient
	{
		string ConnectionMode
		{
			get;
		}

		bool UseBlueBox
		{
			get;
		}

		bool Debug
		{
			get;
		}

		TomOrange Sfs
		{
			get;
		}

		bool Connected
		{
			get;
		}

		IoHandler IoHandler
		{
			get;
			set;
		}

		int CompressionThreshold
		{
			get;
			set;
		}

		int MaxMessageSize
		{
			get;
			set;
		}

		SystemController SysController
		{
			get;
		}

		ExtensionController ExtController
		{
			get;
		}

		ISocketLayer Socket
		{
			get;
		}

		IBBClient HttpClient
		{
			get;
		}

		bool IsReconnecting
		{
			get;
			set;
		}

		int ReconnectionSeconds
		{
			get;
			set;
		}

		EventDispatcher Dispatcher
		{
			get;
			set;
		}

		Logger Log
		{
			get;
		}

		string ConnectionHost
		{
			get;
		}

		int ConnectionPort
		{
			get;
		}

		IUDPManager UdpManager
		{
			get;
			set;
		}

		bool IsBinProtocol
		{
			get;
		}

		CryptoKey CryptoKey
		{
			get;
			set;
		}

		void ForceBlueBox(bool val);

		void Init();

		void Destroy();

		IController GetController(int id);

		void Connect();

		void Connect(string host, int port);

		void Send(IMessage message);

		void Disconnect();

		void Disconnect(string reason);

		void StopReconnection();

		void KillConnection();

		long NextUdpPacketId();

		void AddEventListener(string eventType, EventListenerDelegate listener);

		void EnableBBoxDebug(bool value);
	}
}
