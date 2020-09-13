using Tom.Core;
using Tom.Util;

namespace Tom.Bitswarm.BBox
{
    public interface IBBClient : IDispatchable
    {
        bool IsConnected
        {
            get;
        }

        string SessionId
        {
            get;
        }

        bool IsDebug
        {
            get;
            set;
        }

        void Connect(ConfigData cfg);

        void Send(ByteArray binData);

        void Close(string reason);
    }
}
