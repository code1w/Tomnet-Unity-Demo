using System;
using System.Security.Principal;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp.Net
{
    public sealed class HttpListenerContext
    {
        private HttpConnection _connection;

        private string _error;

        private int _errorStatus;

        private HttpListener _listener;

        private HttpListenerRequest _request;

        private HttpListenerResponse _response;

        private IPrincipal _user;

        private HttpListenerWebSocketContext _websocketContext;

        internal HttpConnection Connection => _connection;

        internal string ErrorMessage
        {
            get
            {
                return _error;
            }
            set
            {
                _error = value;
            }
        }

        internal int ErrorStatus
        {
            get
            {
                return _errorStatus;
            }
            set
            {
                _errorStatus = value;
            }
        }

        internal bool HasError => _error != null;

        internal HttpListener Listener
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

        public HttpListenerRequest Request => _request;

        public HttpListenerResponse Response => _response;

        public IPrincipal User => _user;

        internal HttpListenerContext(HttpConnection connection)
        {
            _connection = connection;
            _errorStatus = 400;
            _request = new HttpListenerRequest(this);
            _response = new HttpListenerResponse(this);
        }

        internal bool Authenticate()
        {
            AuthenticationSchemes authenticationSchemes = _listener.SelectAuthenticationScheme(_request);
            switch (authenticationSchemes)
            {
                case AuthenticationSchemes.Anonymous:
                    return true;
                case AuthenticationSchemes.None:
                    _response.Close(HttpStatusCode.Forbidden);
                    return false;
                default:
                    {
                        string realm = _listener.GetRealm();
                        IPrincipal principal = HttpUtility.CreateUser(_request.Headers["Authorization"], authenticationSchemes, realm, _request.HttpMethod, _listener.GetUserCredentialsFinder());
                        if (principal == null || !principal.Identity.IsAuthenticated)
                        {
                            _response.CloseWithAuthChallenge(new AuthenticationChallenge(authenticationSchemes, realm).ToString());
                            return false;
                        }
                        _user = principal;
                        return true;
                    }
            }
        }

        internal bool Register()
        {
            return _listener.RegisterContext(this);
        }

        internal void Unregister()
        {
            _listener.UnregisterContext(this);
        }

        public HttpListenerWebSocketContext AcceptWebSocket(string protocol)
        {
            if (_websocketContext != null)
            {
                throw new InvalidOperationException("The accepting is already in progress.");
            }
            if (protocol != null)
            {
                if (protocol.Length == 0)
                {
                    throw new ArgumentException("An empty string.", "protocol");
                }
                if (!protocol.IsToken())
                {
                    throw new ArgumentException("Contains an invalid character.", "protocol");
                }
            }
            _websocketContext = new HttpListenerWebSocketContext(this, protocol);
            return _websocketContext;
        }
    }
}
