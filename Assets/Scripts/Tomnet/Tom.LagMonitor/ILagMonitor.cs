namespace Tom.Util.LagMonitor
{
    public interface ILagMonitor
    {
        bool IsRunning
        {
            get;
        }

        void Start();

        void Stop();

        void Destroy();

        void Execute();

        int OnPingPong();
    }
}
