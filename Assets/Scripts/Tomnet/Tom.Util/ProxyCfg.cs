namespace Tom.Util
{
    public class ProxyCfg
    {
        public string Host
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        } = 0;


        public bool BypassLocal
        {
            get;
            set;
        } = true;


        public string UserName
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }
    }
}
