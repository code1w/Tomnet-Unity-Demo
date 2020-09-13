using Tom.Exceptions;
using Tom.Logging;

namespace Tom.Bitswarm
{
    public abstract class BaseController : IController
    {
        protected int id = -1;

        protected TomOrange sfs;

        protected ISocketClient socketClient;

        protected Logger log;

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                if (id == -1)
                {
                    id = value;
                    return;
                }
                throw new SFSError("Controller ID is already set: " + id + ". Can't be changed at runtime!");
            }
        }

        public BaseController(ISocketClient socketClient)
        {
            this.socketClient = socketClient;
            if (socketClient != null)
            {
                log = socketClient.Log;
                sfs = socketClient.Sfs;
            }
        }

        public abstract void HandleMessage(IMessage message);
    }
}
