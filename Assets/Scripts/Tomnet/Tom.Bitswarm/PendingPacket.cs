using Tom.Core;
using Tom.Util;

namespace Tom.Bitswarm
{
	public class PendingPacket
	{
		private PacketHeader header;

		private ByteArray buffer;

		public PacketHeader Header => header;

		public ByteArray Buffer
		{
			get
			{
				return buffer;
			}
			set
			{
				buffer = value;
			}
		}

		public PendingPacket(PacketHeader header)
		{
			this.header = header;
			buffer = new ByteArray();
			buffer.Compressed = header.Compressed;
		}
	}
}
