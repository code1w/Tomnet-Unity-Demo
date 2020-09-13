using System;
using System.Collections.Specialized;
using System.IO;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp.Server
{
    public abstract class WebSocketBehavior : IWebSocketSession
    {
        private WebSocketContext _context;

        private Func<CookieCollection, CookieCollection, bool> _cookiesValidator;

        private bool _emitOnPing;

        private string _id;

        private bool _ignoreExtensions;

        private Func<string, bool> _originValidator;

        private string _protocol;

        private WebSocketSessionManager _sessions;

        private DateTime _startTime;

        private WebSocket _websocket;

        protected NameValueCollection Headers => (_context != null) ? _context.Headers : null;

        [Obsolete("This property will be removed.")]
        protected Logger Log => (_websocket != null) ? _websocket.Log : null;

        protected NameValueCollection QueryString => (_context != null) ? _context.QueryString : null;

        protected WebSocketSessionManager Sessions => _sessions;

        public WebSocketState ConnectionState => (_websocket != null) ? _websocket.ReadyState : WebSocketState.Connecting;

        public WebSocketContext Context => _context;

        public Func<CookieCollection, CookieCollection, bool> CookiesValidator
        {
            get
            {
                return _cookiesValidator;
            }
            set
            {
                _cookiesValidator = value;
            }
        }

        public bool EmitOnPing
        {
            get
            {
                return (_websocket != null) ? _websocket.EmitOnPing : _emitOnPing;
            }
            set
            {
                if (_websocket != null)
                {
                    _websocket.EmitOnPing = value;
                }
                else
                {
                    _emitOnPing = value;
                }
            }
        }

        public string ID => _id;

        public bool IgnoreExtensions
        {
            get
            {
                return _ignoreExtensions;
            }
            set
            {
                _ignoreExtensions = value;
            }
        }

        public Func<string, bool> OriginValidator
        {
            get
            {
                return _originValidator;
            }
            set
            {
                _originValidator = value;
            }
        }

        public string Protocol
        {
            get
            {
                return (_websocket != null) ? _websocket.Protocol : (_protocol ?? string.Empty);
            }
            set
            {
                if (ConnectionState != 0)
                {
                    string message = "The session has already started.";
                    throw new InvalidOperationException(message);
                }
                if (value == null || value.Length == 0)
                {
                    _protocol = null;
                    return;
                }
                if (!value.IsToken())
                {
                    throw new ArgumentException("Not a token.", "value");
                }
                _protocol = value;
            }
        }

        public DateTime StartTime => _startTime;

        protected WebSocketBehavior()
        {
            _startTime = DateTime.MaxValue;
        }

        private string checkHandshakeRequest(WebSocketContext context)
        {
            if (_originValidator != null && !_originValidator(context.Origin))
            {
                return "It includes no Origin header or an invalid one.";
            }
            if (_cookiesValidator != null)
            {
                CookieCollection cookieCollection = context.CookieCollection;
                CookieCollection cookieCollection2 = context.WebSocket.CookieCollection;
                if (!_cookiesValidator(cookieCollection, cookieCollection2))
                {
                    return "It includes no cookie or an invalid one.";
                }
            }
            return null;
        }

        private void onClose(object sender, CloseEventArgs e)
        {
            if (_id != null)
            {
                _sessions.Remove(_id);
                OnClose(e);
            }
        }

        private void onError(object sender, ErrorEventArgs e)
        {
            OnError(e);
        }

        private void onMessage(object sender, MessageEventArgs e)
        {
            OnMessage(e);
        }

        private void onOpen(object sender, EventArgs e)
        {
            _id = _sessions.Add(this);
            if (_id == null)
            {
                _websocket.Close(CloseStatusCode.Away);
                return;
            }
            _startTime = DateTime.Now;
            OnOpen();
        }

        internal void Start(WebSocketContext context, WebSocketSessionManager sessions)
        {
            if (_websocket != null)
            {
                _websocket.Log.Error("A session instance cannot be reused.");
                context.WebSocket.Close(HttpStatusCode.ServiceUnavailable);
                return;
            }
            _context = context;
            _sessions = sessions;
            _websocket = context.WebSocket;
            _websocket.CustomHandshakeRequestChecker = checkHandshakeRequest;
            _websocket.EmitOnPing = _emitOnPing;
            _websocket.IgnoreExtensions = _ignoreExtensions;
            _websocket.Protocol = _protocol;
            TimeSpan waitTime = sessions.WaitTime;
            if (waitTime != _websocket.WaitTime)
            {
                _websocket.WaitTime = waitTime;
            }
            _websocket.OnOpen += onOpen;
            _websocket.OnMessage += onMessage;
            _websocket.OnError += onError;
            _websocket.OnClose += onClose;
            _websocket.InternalAccept();
        }

        protected void Close()
        {
            if (_websocket == null)
            {
                string message = "The session has not started yet.";
                throw new InvalidOperationException(message);
            }
            _websocket.Close();
        }

        protected void Close(ushort code, string reason)
        {
            if (_websocket == null)
            {
                string message = "The session has not started yet.";
                throw new InvalidOperationException(message);
            }
            _websocket.Close(code, reason);
        }

        protected void Close(CloseStatusCode code, string reason)
        {
            if (_websocket == null)
            {
                string message = "The session has not started yet.";
                throw new InvalidOperationException(message);
            }
            _websocket.Close(code, reason);
        }

        protected void CloseAsync()
        {
            if (_websocket == null)
            {
                string message = "The session has not started yet.";
                throw new InvalidOperationException(message);
            }
            _websocket.CloseAsync();
        }

        protected void CloseAsync(ushort code, string reason)
        {
            if (_websocket == null)
            {
                string message = "The session has not started yet.";
                throw new InvalidOperationException(message);
            }
            _websocket.CloseAsync(code, reason);
        }

        protected void CloseAsync(CloseStatusCode code, string reason)
        {
            if (_websocket == null)
            {
                string message = "The session has not started yet.";
                throw new InvalidOperationException(message);
            }
            _websocket.CloseAsync(code, reason);
        }

        [Obsolete("This method will be removed.")]
        protected void Error(string message, Exception exception)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            if (message.Length == 0)
            {
                throw new ArgumentException("An empty string.", "message");
            }
            OnError(new ErrorEventArgs(message, exception));
        }

        protected virtual void OnClose(CloseEventArgs e)
        {
        }

        protected virtual void OnError(ErrorEventArgs e)
        {
        }

        protected virtual void OnMessage(MessageEventArgs e)
        {
        }

        protected virtual void OnOpen()
        {
        }

        protected void Send(byte[] data)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.Send(data);
        }

        protected void Send(FileInfo fileInfo)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.Send(fileInfo);
        }

        protected void Send(string data)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.Send(data);
        }

        protected void Send(Stream stream, int length)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.Send(stream, length);
        }

        protected void SendAsync(byte[] data, Action<bool> completed)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.SendAsync(data, completed);
        }

        protected void SendAsync(FileInfo fileInfo, Action<bool> completed)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.SendAsync(fileInfo, completed);
        }

        protected void SendAsync(string data, Action<bool> completed)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.SendAsync(data, completed);
        }

        protected void SendAsync(Stream stream, int length, Action<bool> completed)
        {
            if (_websocket == null)
            {
                string message = "The current state of the connection is not Open.";
                throw new InvalidOperationException(message);
            }
            _websocket.SendAsync(stream, length, completed);
        }
    }
}
