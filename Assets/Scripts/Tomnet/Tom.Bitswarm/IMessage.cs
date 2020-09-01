using Tom.Entities.Data;

namespace Tom.Bitswarm
{
	public interface IMessage
	{
		int Id
		{
			get;
			set;
		}

		ISFSObject Content
		{
			get;
			set;
		}

		int TargetController
		{
			get;
			set;
		}

		bool IsEncrypted
		{
			get;
			set;
		}

		bool IsUDP
		{
			get;
			set;
		}

		long PacketId
		{
			get;
			set;
		}
	}
}
