using TomNet.Common;
using TomNet.Protocol;

namespace TomNet.Core
{
    public delegate void WriteBinaryDataDelegate(IMsgHeader header, ByteBuffer binData, bool udp);
}
