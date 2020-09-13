using System;

namespace WebSocketSharp.Net
{
    internal sealed class HttpListenerPrefix
    {
        private string _host;

        private HttpListener _listener;

        private string _original;

        private string _path;

        private string _port;

        private string _prefix;

        private bool _secure;

        public string Host => _host;

        public bool IsSecure => _secure;

        public HttpListener Listener
        {
            get
            {
                return _listener;
            }
            set
            {
                _listener = value;
            }
        }

        public string Original => _original;

        public string Path => _path;

        public string Port => _port;

        internal HttpListenerPrefix(string uriPrefix)
        {
            _original = uriPrefix;
            parse(uriPrefix);
        }

        private void parse(string uriPrefix)
        {
            if (uriPrefix.StartsWith("https"))
            {
                _secure = true;
            }
            int length = uriPrefix.Length;
            int num = uriPrefix.IndexOf(':') + 3;
            int num2 = uriPrefix.IndexOf('/', num + 1, length - num - 1);
            int num3 = uriPrefix.LastIndexOf(':', num2 - 1, num2 - num - 1);
            if (uriPrefix[num2 - 1] != ']' && num3 > num)
            {
                _host = uriPrefix.Substring(num, num3 - num);
                _port = uriPrefix.Substring(num3 + 1, num2 - num3 - 1);
            }
            else
            {
                _host = uriPrefix.Substring(num, num2 - num);
                _port = (_secure ? "443" : "80");
            }
            _path = uriPrefix.Substring(num2);
            _prefix = string.Format("http{0}://{1}:{2}{3}", _secure ? "s" : "", _host, _port, _path);
        }

        public static void CheckPrefix(string uriPrefix)
        {
            if (uriPrefix == null)
            {
                throw new ArgumentNullException("uriPrefix");
            }
            int length = uriPrefix.Length;
            if (length == 0)
            {
                throw new ArgumentException("An empty string.", "uriPrefix");
            }
            if (!uriPrefix.StartsWith("http://") && !uriPrefix.StartsWith("https://"))
            {
                throw new ArgumentException("The scheme isn't 'http' or 'https'.", "uriPrefix");
            }
            int num = uriPrefix.IndexOf(':') + 3;
            if (num >= length)
            {
                throw new ArgumentException("No host is specified.", "uriPrefix");
            }
            if (uriPrefix[num] == ':')
            {
                throw new ArgumentException("No host is specified.", "uriPrefix");
            }
            int num2 = uriPrefix.IndexOf('/', num, length - num);
            if (num2 == num)
            {
                throw new ArgumentException("No host is specified.", "uriPrefix");
            }
            if (num2 == -1 || uriPrefix[length - 1] != '/')
            {
                throw new ArgumentException("Ends without '/'.", "uriPrefix");
            }
            if (uriPrefix[num2 - 1] == ':')
            {
                throw new ArgumentException("No port is specified.", "uriPrefix");
            }
            if (num2 == length - 2)
            {
                throw new ArgumentException("No path is specified.", "uriPrefix");
            }
        }

        public override bool Equals(object obj)
        {
            HttpListenerPrefix httpListenerPrefix = obj as HttpListenerPrefix;
            return httpListenerPrefix != null && httpListenerPrefix._prefix == _prefix;
        }

        public override int GetHashCode()
        {
            return _prefix.GetHashCode();
        }

        public override string ToString()
        {
            return _prefix;
        }
    }
}
