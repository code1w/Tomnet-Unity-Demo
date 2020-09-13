using Tom.Bitswarm;

namespace Tom.Requests
{
    public interface IRequest
    {
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

        IMessage Message
        {
            get;
        }

        void Validate(TomOrange sfs);

        void Execute(TomOrange sfs);
    }
}
