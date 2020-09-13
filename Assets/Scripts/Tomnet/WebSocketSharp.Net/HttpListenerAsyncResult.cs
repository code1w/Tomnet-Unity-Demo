using System;
using System.Threading;

namespace WebSocketSharp.Net
{
    internal class HttpListenerAsyncResult : IAsyncResult
    {
        private AsyncCallback _callback;

        private bool _completed;

        private HttpListenerContext _context;

        private bool _endCalled;

        private Exception _exception;

        private bool _inGet;

        private object _state;

        private object _sync;

        private bool _syncCompleted;

        private ManualResetEvent _waitHandle;

        internal bool EndCalled
        {
            get
            {
                return _endCalled;
            }
            set
            {
                _endCalled = value;
            }
        }

        internal bool InGet
        {
            get
            {
                return _inGet;
            }
            set
            {
                _inGet = value;
            }
        }

        public object AsyncState => _state;

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (_sync)
                {
                    return _waitHandle ?? (_waitHandle = new ManualResetEvent(_completed));
                }
            }
        }

        public bool CompletedSynchronously => _syncCompleted;

        public bool IsCompleted
        {
            get
            {
                lock (_sync)
                {
                    return _completed;
                }
            }
        }

        internal HttpListenerAsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            _state = state;
            _sync = new object();
        }

        private static void complete(HttpListenerAsyncResult asyncResult)
        {
            lock (asyncResult._sync)
            {
                asyncResult._completed = true;
                asyncResult._waitHandle?.Set();
            }
            AsyncCallback callback = asyncResult._callback;
            if (callback == null)
            {
                return;
            }
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    callback(asyncResult);
                }
                catch
                {
                }
            }, null);
        }

        internal void Complete(Exception exception)
        {
            _exception = ((_inGet && exception is ObjectDisposedException) ? new HttpListenerException(995, "The listener is closed.") : exception);
            complete(this);
        }

        internal void Complete(HttpListenerContext context)
        {
            Complete(context, syncCompleted: false);
        }

        internal void Complete(HttpListenerContext context, bool syncCompleted)
        {
            _context = context;
            _syncCompleted = syncCompleted;
            complete(this);
        }

        internal HttpListenerContext GetContext()
        {
            if (_exception != null)
            {
                throw _exception;
            }
            return _context;
        }
    }
}
