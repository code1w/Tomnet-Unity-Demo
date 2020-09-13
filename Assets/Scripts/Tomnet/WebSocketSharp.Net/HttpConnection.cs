using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebSocketSharp.Net
{
    internal sealed class HttpConnection
    {
        private byte[] _buffer;

        private const int _bufferLength = 8192;

        private HttpListenerContext _context;

        private bool _contextRegistered;

        private StringBuilder _currentLine;

        private InputState _inputState;

        private RequestStream _inputStream;

        private HttpListener _lastListener;

        private LineState _lineState;

        private EndPointListener _listener;

        private EndPoint _localEndPoint;

        private ResponseStream _outputStream;

        private int _position;

        private EndPoint _remoteEndPoint;

        private MemoryStream _requestBuffer;

        private int _reuses;

        private bool _secure;

        private Socket _socket;

        private Stream _stream;

        private object _sync;

        private int _timeout;

        private Dictionary<int, bool> _timeoutCanceled;

        private Timer _timer;

        public bool IsClosed => _socket == null;

        public bool IsLocal => ((IPEndPoint)_remoteEndPoint).Address.IsLocal();

        public bool IsSecure => _secure;

        public IPEndPoint LocalEndPoint => (IPEndPoint)_localEndPoint;

        public IPEndPoint RemoteEndPoint => (IPEndPoint)_remoteEndPoint;

        public int Reuses => _reuses;

        public Stream Stream => _stream;

        internal HttpConnection(Socket socket, EndPointListener listener)
        {
            _socket = socket;
            _listener = listener;
            NetworkStream networkStream = new NetworkStream(socket, ownsSocket: false);
            if (listener.IsSecure)
            {
                ServerSslConfiguration sslConfiguration = listener.SslConfiguration;
                SslStream sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false, sslConfiguration.ClientCertificateValidationCallback);
                sslStream.AuthenticateAsServer(sslConfiguration.ServerCertificate, sslConfiguration.ClientCertificateRequired, sslConfiguration.EnabledSslProtocols, sslConfiguration.CheckCertificateRevocation);
                _secure = true;
                _stream = sslStream;
            }
            else
            {
                _stream = networkStream;
            }
            _localEndPoint = socket.LocalEndPoint;
            _remoteEndPoint = socket.RemoteEndPoint;
            _sync = new object();
            _timeout = 90000;
            _timeoutCanceled = new Dictionary<int, bool>();
            _timer = new Timer(onTimeout, this, -1, -1);
            init();
        }

        private void close()
        {
            lock (_sync)
            {
                if (_socket == null)
                {
                    return;
                }
                disposeTimer();
                disposeRequestBuffer();
                disposeStream();
                closeSocket();
            }
            unregisterContext();
            removeConnection();
        }

        private void closeSocket()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }
            _socket.Close();
            _socket = null;
        }

        private void disposeRequestBuffer()
        {
            if (_requestBuffer != null)
            {
                _requestBuffer.Dispose();
                _requestBuffer = null;
            }
        }

        private void disposeStream()
        {
            if (_stream != null)
            {
                _inputStream = null;
                _outputStream = null;
                _stream.Dispose();
                _stream = null;
            }
        }

        private void disposeTimer()
        {
            if (_timer != null)
            {
                try
                {
                    _timer.Change(-1, -1);
                }
                catch
                {
                }
                _timer.Dispose();
                _timer = null;
            }
        }

        private void init()
        {
            _context = new HttpListenerContext(this);
            _inputState = InputState.RequestLine;
            _inputStream = null;
            _lineState = LineState.None;
            _outputStream = null;
            _position = 0;
            _requestBuffer = new MemoryStream();
        }

        private static void onRead(IAsyncResult asyncResult)
        {
            HttpConnection httpConnection = (HttpConnection)asyncResult.AsyncState;
            if (httpConnection._socket == null)
            {
                return;
            }
            lock (httpConnection._sync)
            {
                if (httpConnection._socket == null)
                {
                    return;
                }
                int num = -1;
                int num2 = 0;
                try
                {
                    int reuses = httpConnection._reuses;
                    if (!httpConnection._timeoutCanceled[reuses])
                    {
                        httpConnection._timer.Change(-1, -1);
                        httpConnection._timeoutCanceled[reuses] = true;
                    }
                    num = httpConnection._stream.EndRead(asyncResult);
                    httpConnection._requestBuffer.Write(httpConnection._buffer, 0, num);
                    num2 = (int)httpConnection._requestBuffer.Length;
                }
                catch (Exception ex)
                {
                    if (httpConnection._requestBuffer != null && httpConnection._requestBuffer.Length > 0)
                    {
                        httpConnection.SendError(ex.Message, 400);
                    }
                    else
                    {
                        httpConnection.close();
                    }
                    return;
                }
                HttpListener listener;
                if (num <= 0)
                {
                    httpConnection.close();
                }
                else if (httpConnection.processInput(httpConnection._requestBuffer.GetBuffer(), num2))
                {
                    if (!httpConnection._context.HasError)
                    {
                        httpConnection._context.Request.FinishInitialization();
                    }
                    if (httpConnection._context.HasError)
                    {
                        httpConnection.SendError();
                        return;
                    }
                    if (!httpConnection._listener.TrySearchHttpListener(httpConnection._context.Request.Url, out listener))
                    {
                        httpConnection.SendError(null, 404);
                        return;
                    }
                    if (httpConnection._lastListener == listener)
                    {
                        goto IL_01f9;
                    }
                    httpConnection.removeConnection();
                    if (listener.AddConnection(httpConnection))
                    {
                        httpConnection._lastListener = listener;
                        goto IL_01f9;
                    }
                    httpConnection.close();
                }
                else
                {
                    httpConnection._stream.BeginRead(httpConnection._buffer, 0, 8192, onRead, httpConnection);
                }
                goto end_IL_0028;
            IL_01f9:
                httpConnection._context.Listener = listener;
                if (httpConnection._context.Authenticate() && httpConnection._context.Register())
                {
                    httpConnection._contextRegistered = true;
                }
            end_IL_0028:;
            }
        }

        private static void onTimeout(object state)
        {
            HttpConnection httpConnection = (HttpConnection)state;
            int reuses = httpConnection._reuses;
            if (httpConnection._socket == null)
            {
                return;
            }
            lock (httpConnection._sync)
            {
                if (httpConnection._socket != null && !httpConnection._timeoutCanceled[reuses])
                {
                    httpConnection.SendError(null, 408);
                }
            }
        }

        private bool processInput(byte[] data, int length)
        {
            if (_currentLine == null)
            {
                _currentLine = new StringBuilder(64);
            }
            int read = 0;
            try
            {
                string text;
                while ((text = readLineFrom(data, _position, length, out read)) != null)
                {
                    _position += read;
                    if (text.Length == 0)
                    {
                        if (_inputState != 0)
                        {
                            if (_position > 32768)
                            {
                                _context.ErrorMessage = "Headers too long";
                            }
                            _currentLine = null;
                            return true;
                        }
                    }
                    else
                    {
                        if (_inputState == InputState.RequestLine)
                        {
                            _context.Request.SetRequestLine(text);
                            _inputState = InputState.Headers;
                        }
                        else
                        {
                            _context.Request.AddHeader(text);
                        }
                        if (_context.HasError)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _context.ErrorMessage = ex.Message;
                return true;
            }
            _position += read;
            if (_position >= 32768)
            {
                _context.ErrorMessage = "Headers too long";
                return true;
            }
            return false;
        }

        private string readLineFrom(byte[] buffer, int offset, int length, out int read)
        {
            read = 0;
            for (int i = offset; i < length; i++)
            {
                if (_lineState == LineState.Lf)
                {
                    break;
                }
                read++;
                byte b = buffer[i];
                switch (b)
                {
                    case 13:
                        _lineState = LineState.Cr;
                        break;
                    case 10:
                        _lineState = LineState.Lf;
                        break;
                    default:
                        _currentLine.Append((char)b);
                        break;
                }
            }
            if (_lineState != LineState.Lf)
            {
                return null;
            }
            string result = _currentLine.ToString();
            _currentLine.Length = 0;
            _lineState = LineState.None;
            return result;
        }

        private void removeConnection()
        {
            if (_lastListener != null)
            {
                _lastListener.RemoveConnection(this);
            }
            else
            {
                _listener.RemoveConnection(this);
            }
        }

        private void unregisterContext()
        {
            if (_contextRegistered)
            {
                _context.Unregister();
                _contextRegistered = false;
            }
        }

        internal void Close(bool force)
        {
            if (_socket == null)
            {
                return;
            }
            lock (_sync)
            {
                if (_socket == null)
                {
                    return;
                }
                if (force)
                {
                    if (_outputStream != null)
                    {
                        _outputStream.Close(force: true);
                    }
                    close();
                    return;
                }
                GetResponseStream().Close(force: false);
                if (_context.Response.CloseConnection)
                {
                    close();
                    return;
                }
                if (!_context.Request.FlushInput())
                {
                    close();
                    return;
                }
                disposeRequestBuffer();
                unregisterContext();
                init();
                _reuses++;
                BeginReadRequest();
            }
        }

        public void BeginReadRequest()
        {
            if (_buffer == null)
            {
                _buffer = new byte[8192];
            }
            if (_reuses == 1)
            {
                _timeout = 15000;
            }
            try
            {
                _timeoutCanceled.Add(_reuses, value: false);
                _timer.Change(_timeout, -1);
                _stream.BeginRead(_buffer, 0, 8192, onRead, this);
            }
            catch
            {
                close();
            }
        }

        public void Close()
        {
            Close(force: false);
        }

        public RequestStream GetRequestStream(long contentLength, bool chunked)
        {
            lock (_sync)
            {
                if (_socket == null)
                {
                    return null;
                }
                if (_inputStream != null)
                {
                    return _inputStream;
                }
                byte[] buffer = _requestBuffer.GetBuffer();
                int num = (int)_requestBuffer.Length;
                int count = num - _position;
                disposeRequestBuffer();
                _inputStream = (chunked ? new ChunkedRequestStream(_stream, buffer, _position, count, _context) : new RequestStream(_stream, buffer, _position, count, contentLength));
                return _inputStream;
            }
        }

        public ResponseStream GetResponseStream()
        {
            lock (_sync)
            {
                if (_socket == null)
                {
                    return null;
                }
                if (_outputStream != null)
                {
                    return _outputStream;
                }
                bool ignoreWriteExceptions = _context.Listener?.IgnoreWriteExceptions ?? true;
                _outputStream = new ResponseStream(_stream, _context.Response, ignoreWriteExceptions);
                return _outputStream;
            }
        }

        public void SendError()
        {
            SendError(_context.ErrorMessage, _context.ErrorStatus);
        }

        public void SendError(string message, int status)
        {
            if (_socket == null)
            {
                return;
            }
            lock (_sync)
            {
                if (_socket == null)
                {
                    return;
                }
                try
                {
                    HttpListenerResponse response = _context.Response;
                    response.StatusCode = status;
                    response.ContentType = "text/html";
                    StringBuilder stringBuilder = new StringBuilder(64);
                    stringBuilder.AppendFormat("<html><body><h1>{0} {1}", status, response.StatusDescription);
                    if (message != null && message.Length > 0)
                    {
                        stringBuilder.AppendFormat(" ({0})</h1></body></html>", message);
                    }
                    else
                    {
                        stringBuilder.Append("</h1></body></html>");
                    }
                    Encoding uTF = Encoding.UTF8;
                    byte[] bytes = uTF.GetBytes(stringBuilder.ToString());
                    response.ContentEncoding = uTF;
                    response.ContentLength64 = bytes.LongLength;
                    response.Close(bytes, willBlock: true);
                }
                catch
                {
                    Close(force: true);
                }
            }
        }
    }
}
