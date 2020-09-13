using Tom.Bitswarm;
using Tom.Entities.Data;
using Tom.Util;

namespace Tom.Protocol
{
    public interface IProtocolCodec
    {
        IoHandler IOHandler
        {
            get;
            set;
        }

        void OnPacketRead(ISFSObject packet);

        void OnPacketRead(ByteArray packet);

        void OnPacketWrite(IMessage message);
    }
}
