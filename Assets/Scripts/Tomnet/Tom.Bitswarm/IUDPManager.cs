using Tom.Util;

namespace Tom.Bitswarm
{
	public interface IUDPManager
	{
		bool Inited
		{
			get;
		}

		long NextUdpPacketId
		{
			get;
		}

		void Initialize(string udpAddr, int udpPort);

		void Send(ByteArray binaryData);

		void Reset();

		void Disconnect();

		bool isConnected();
	}
}
