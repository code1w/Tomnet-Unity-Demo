using Tom.Protocol;
using Tom.Util;

namespace Tom.Bitswarm
{
    public interface IoHandler
    {
        IProtocolCodec Codec
        {
            get;
        }

        void OnDataRead(ByteArray buffer);

        void OnDataRead(string jsonData);

        void OnDataWrite(IMessage message);
    }
}
