using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;

namespace WebSocketSharp.Net
{
    public sealed class HttpListener : IDisposable
    {
        private AuthenticationSchemes _authSchemes;

        private Func<HttpListenerRequest, AuthenticationSchemes> _authSchemeSelector;

        private string _certFolderPath;

        private Dictionary<HttpConnection, HttpConnection> _connections;

        private object _connectionsSync;

        private List<HttpListenerContext> _ctxQueue;

        private object _ctxQueueSync;

        private Dictionary<HttpListenerContext, HttpListenerContext> _ctxRegistry;

        private object _ctxRegistrySync;

        private static readonly string _defaultRealm;

        private bool _disposed;

        private bool _ignoreWriteExceptions;

        private volatile bool _listening;

        private Logger _logger;

        private HttpListenerPrefixCollection _prefixes;

        private string _realm;

        private bool _reuseAddress;

        private ServerSslConfiguration _sslConfig;

        private Func<IIdentity, NetworkCredential> _userCredFinder;

        private List<HttpListenerAsyncResult> _waitQueue;

        private object _waitQueueSync;

        internal bool IsDisposed => _disposed;

        internal bool ReuseAddress
        {
            get
            {
                return _reuseAddress;
            }
            set
            {
                _reuseAddress = value;
            }
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                CheckDisposed();
                return _authSchemes;
            }
            set
            {
                CheckDisposed();
                _authSchemes = value;
            }
        }

        public Func<HttpListenerRequest, AuthenticationSchemes> AuthenticationSchemeSelector
        {
            get
            {
                CheckDisposed();
                return _authSchemeSelector;
            }
            set
            {
                CheckDisposed();
                _authSchemeSelector = value;
            }
        }

        public string CertificateFolderPath
        {
            get
            {
                CheckDisposed();
                return _certFolderPath;
            }
            set
            {
                CheckDisposed();
                _certFolderPath = value;
            }
        }

        public bool IgnoreWriteExceptions
        {
            get
            {
                CheckDisposed();
                return _ignoreWriteExceptions;
            }
            set
            {
                CheckDisposed();
                _ignoreWriteExceptions = value;
            }
        }

        public bool IsListening => _listening;

        public static bool IsSupported => true;

        public Logger Log => _logger;

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                CheckDisposed();
                return _prefixes;
            }
        }

        public string Realm
        {
            get
            {
                CheckDisposed();
                return _realm;
            }
            set
            {
                CheckDisposed();
                _realm = value;
            }
        }

        public ServerSslConfiguration SslConfiguration
        {
            get
            {
                CheckDisposed();
                return _sslConfig ?? (_sslConfig = new ServerSslConfiguration());
            }
            set
            {
                CheckDisposed();
                _sslConfig = value;
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public Func<IIdentity, NetworkCredential> UserCredentialsFinder
        {
            get
            {
                CheckDisposed();
                return _userCredFinder;
            }
            set
            {
                CheckDisposed();
                _userCredFinder = value;
            }
        }

        static HttpListener()
        {
            _defaultRealm = "SECRET AREA";
        }

        public HttpListener()
        {
            _authSchemes = AuthenticationSchemes.Anonymous;
            _connections = new Dictionary<HttpConnection, HttpConnection>();
            _connectionsSync = ((ICollection)_connections).SyncRoot;
            _ctxQueue = new List<HttpListenerContext>();
            _ctxQueueSync = ((ICollection)_ctxQueue).SyncRoot;
            _ctxRegistry = new Dictionary<HttpListenerContext, HttpListenerContext>();
            _ctxRegistrySync = ((ICollection)_ctxRegistry).SyncRoot;
            _logger = new Logger();
            _prefixes = new HttpListenerPrefixCollection(this);
            _waitQueue = new List<HttpListenerAsyncResult>();
            _waitQueueSync = ((ICollection)_waitQueue).SyncRoot;
        }

        private void cleanupConnections()
        {
            HttpConnection[] array = null;
            lock (_connectionsSync)
            {
                if (_connections.Count == 0)
                {
                    return;
                }
                Dictionary<HttpConnection, HttpConnection>.KeyCollection keys = _connections.Keys;
                array = new HttpConnection[keys.Count];
                keys.CopyTo(array, 0);
                _connections.Clear();
            }
            for (int num = array.Length - 1; num >= 0; num--)
            {
                array[num].Close(force: true);
            }
        }

        private void cleanupContextQueue(bool sendServiceUnavailable)
        {
            HttpListenerContext[] array = null;
            lock (_ctxQueueSync)
            {
                if (_ctxQueue.Count == 0)
                {
                    return;
                }
                array = _ctxQueue.ToArray();
                _ctxQueue.Clear();
            }
            if (sendServiceUnavailable)
            {
                HttpListenerContext[] array2 = array;
                foreach (HttpListenerContext httpListenerContext in array2)
                {
                    HttpListenerResponse response = httpListenerContext.Response;
                    response.StatusCode = 503;
                    response.Close();
                }
            }
        }

        private void cleanupContextRegistry()
        {
            HttpListenerContext[] array = null;
            lock (_ctxRegistrySync)
            {
                if (_ctxRegistry.Count == 0)
                {
                    return;
                }
                Dictionary<HttpListenerContext, HttpListenerContext>.KeyCollection keys = _ctxRegistry.Keys;
                array = new HttpListenerContext[keys.Count];
                keys.CopyTo(array, 0);
                _ctxRegistry.Clear();
            }
            for (int num = array.Length - 1; num >= 0; num--)
            {
                array[num].Connection.Close(force: true);
            }
        }

        private void cleanupWaitQueue(Exception exception)
        {
            HttpListenerAsyncResult[] array = null;
            lock (_waitQueueSync)
            {
                if (_waitQueue.Count == 0)
                {
                    return;
                }
                array = _waitQueue.ToArray();
                _waitQueue.Clear();
            }
            HttpListenerAsyncResult[] array2 = array;
            foreach (HttpListenerAsyncResult httpListenerAsyncResult in array2)
            {
                httpListenerAsyncResult.Complete(exception);
            }
        }

        private void close(bool force)
        {
            if (_listening)
            {
                _listening = false;
                EndPointManager.RemoveListener(this);
            }
            lock (_ctxRegistrySync)
            {
                cleanupContextQueue(!force);
            }
            cleanupContextRegistry();
            cleanupConnections();
            cleanupWaitQueue(new ObjectDisposedException(GetType().ToString()));
            _disposed = true;
        }

        private HttpListenerAsyncResult getAsyncResultFromQueue()
        {
            if (_waitQueue.Count == 0)
            {
                return null;
            }
            HttpListenerAsyncResult result = _waitQueue[0];
            _waitQueue.RemoveAt(0);
            return result;
        }

        private HttpListenerContext getContextFromQueue()
        {
            if (_ctxQueue.Count == 0)
            {
                return null;
            }
            HttpListenerContext result = _ctxQueue[0];
            _ctxQueue.RemoveAt(0);
            return result;
        }

        internal bool AddConnection(HttpConnection connection)
        {
            if (!_listening)
            {
                return false;
            }
            lock (_connectionsSync)
            {
                if (!_listening)
                {
                    return false;
                }
                _connections[connection] = connection;
                return true;
            }
        }

        internal HttpListenerAsyncResult BeginGetContext(HttpListenerAsyncResult asyncResult)
        {
            lock (_ctxRegistrySync)
            {
                if (!_listening)
                {
                    throw new HttpListenerException(995);
                }
                HttpListenerContext contextFromQueue = getContextFromQueue();
                if (contextFromQueue == null)
                {
                    _waitQueue.Add(asyncResult);
                }
                else
                {
                    asyncResult.Complete(contextFromQueue, syncCompleted: true);
                }
                return asyncResult;
            }
        }

        internal void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        internal string GetRealm()
        {
            string realm = _realm;
            return (realm != null && realm.Length > 0) ? realm : _defaultRealm;
        }

        internal Func<IIdentity, NetworkCredential> GetUserCredentialsFinder()
        {
            return _userCredFinder;
        }

        internal bool RegisterContext(HttpListenerContext context)
        {
            if (!_listening)
            {
                return false;
            }
            lock (_ctxRegistrySync)
            {
                if (!_listening)
                {
                    return false;
                }
                _ctxRegistry[context] = context;
                HttpListenerAsyncResult asyncResultFromQueue = getAsyncResultFromQueue();
                if (asyncResultFromQueue == null)
                {
                    _ctxQueue.Add(context);
                }
                else
                {
                    asyncResultFromQueue.Complete(context);
                }
                return true;
            }
        }

        internal void RemoveConnection(HttpConnection connection)
        {
            lock (_connectionsSync)
            {
                _connections.Remove(connection);
            }
        }

        internal AuthenticationSchemes SelectAuthenticationScheme(HttpListenerRequest request)
        {
            Func<HttpListenerRequest, AuthenticationSchemes> authSchemeSelector = _authSchemeSelector;
            if (authSchemeSelector == null)
            {
                return _authSchemes;
            }
            try
            {
                return authSchemeSelector(request);
            }
            catch
            {
                return AuthenticationSchemes.None;
            }
        }

        internal void UnregisterContext(HttpListenerContext context)
        {
            lock (_ctxRegistrySync)
            {
                _ctxRegistry.Remove(context);
            }
        }

        public void Abort()
        {
            if (!_disposed)
            {
                close(force: true);
            }
        }

        public IAsyncResult BeginGetContext(AsyncCallback callback, object state)
        {
            CheckDisposed();
            if (_prefixes.Count == 0)
            {
                throw new InvalidOperationException("The listener has no URI prefix on which listens.");
            }
            if (!_listening)
            {
                throw new InvalidOperationException("The listener hasn't been started.");
            }
            return BeginGetContext(new HttpListenerAsyncResult(callback, state));
        }

        public void Close()
        {
            if (!_disposed)
            {
                close(force: false);
            }
        }

        public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
        {
            CheckDisposed();
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            HttpListenerAsyncResult httpListenerAsyncResult = asyncResult as HttpListenerAsyncResult;
            if (httpListenerAsyncResult == null)
            {
                throw new ArgumentException("A wrong IAsyncResult.", "asyncResult");
            }
            if (httpListenerAsyncResult.EndCalled)
            {
                throw new InvalidOperationException("This IAsyncResult cannot be reused.");
            }
            httpListenerAsyncResult.EndCalled = true;
            if (!httpListenerAsyncResult.IsCompleted)
            {
                httpListenerAsyncResult.AsyncWaitHandle.WaitOne();
            }
            return httpListenerAsyncResult.GetContext();
        }

        public HttpListenerContext GetContext()
        {
            CheckDisposed();
            if (_prefixes.Count == 0)
            {
                throw new InvalidOperationException("The listener has no URI prefix on which listens.");
            }
            if (!_listening)
            {
                throw new InvalidOperationException("The listener hasn't been started.");
            }
            HttpListenerAsyncResult httpListenerAsyncResult = BeginGetContext(new HttpListenerAsyncResult(null, null));
            httpListenerAsyncResult.InGet = true;
            return EndGetContext(httpListenerAsyncResult);
        }

        public void Start()
        {
            CheckDisposed();
            if (!_listening)
            {
                EndPointManager.AddListener(this);
                _listening = true;
            }
        }

        public void Stop()
        {
            CheckDisposed();
            if (_listening)
            {
                _listening = false;
                EndPointManager.RemoveListener(this);
                lock (_ctxRegistrySync)
                {
                    cleanupContextQueue(sendServiceUnavailable: true);
                }
                cleanupContextRegistry();
                cleanupConnections();
                cleanupWaitQueue(new HttpListenerException(995, "The listener is stopped."));
            }
        }

        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                close(force: true);
            }
        }
    }
}
