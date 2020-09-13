using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace WebSocketSharp.Net
{
    public sealed class HttpListenerResponse : IDisposable
    {
        private bool _closeConnection;

        private Encoding _contentEncoding;

        private long _contentLength;

        private string _contentType;

        private HttpListenerContext _context;

        private CookieCollection _cookies;

        private bool _disposed;

        private WebHeaderCollection _headers;

        private bool _headersSent;

        private bool _keepAlive;

        private string _location;

        private ResponseStream _outputStream;

        private bool _sendChunked;

        private int _statusCode;

        private string _statusDescription;

        private Version _version;

        internal bool CloseConnection
        {
            get
            {
                return _closeConnection;
            }
            set
            {
                _closeConnection = value;
            }
        }

        internal bool HeadersSent
        {
            get
            {
                return _headersSent;
            }
            set
            {
                _headersSent = value;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                return _contentEncoding;
            }
            set
            {
                checkDisposed();
                _contentEncoding = value;
            }
        }

        public long ContentLength64
        {
            get
            {
                return _contentLength;
            }
            set
            {
                checkDisposedOrHeadersSent();
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Less than zero.", "value");
                }
                _contentLength = value;
            }
        }

        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                checkDisposed();
                if (value != null && value.Length == 0)
                {
                    throw new ArgumentException("An empty string.", "value");
                }
                _contentType = value;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                return _cookies ?? (_cookies = new CookieCollection());
            }
            set
            {
                _cookies = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return _headers ?? (_headers = new WebHeaderCollection(HttpHeaderType.Response, internallyUsed: false));
            }
            set
            {
                if (value != null && value.State != HttpHeaderType.Response)
                {
                    throw new InvalidOperationException("The specified headers aren't valid for a response.");
                }
                _headers = value;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return _keepAlive;
            }
            set
            {
                checkDisposedOrHeadersSent();
                _keepAlive = value;
            }
        }

        public Stream OutputStream
        {
            get
            {
                checkDisposed();
                return _outputStream ?? (_outputStream = _context.Connection.GetResponseStream());
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return _version;
            }
            set
            {
                checkDisposedOrHeadersSent();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                {
                    throw new ArgumentException("Not 1.0 or 1.1.", "value");
                }
                _version = value;
            }
        }

        public string RedirectLocation
        {
            get
            {
                return _location;
            }
            set
            {
                checkDisposed();
                if (value == null)
                {
                    _location = null;
                    return;
                }
                Uri result = null;
                if (!value.MaybeUri() || !Uri.TryCreate(value, UriKind.Absolute, out result))
                {
                    throw new ArgumentException("Not an absolute URL.", "value");
                }
                _location = value;
            }
        }

        public bool SendChunked
        {
            get
            {
                return _sendChunked;
            }
            set
            {
                checkDisposedOrHeadersSent();
                _sendChunked = value;
            }
        }

        public int StatusCode
        {
            get
            {
                return _statusCode;
            }
            set
            {
                checkDisposedOrHeadersSent();
                if (value < 100 || value > 999)
                {
                    throw new ProtocolViolationException("A value isn't between 100 and 999 inclusive.");
                }
                _statusCode = value;
                _statusDescription = value.GetStatusDescription();
            }
        }

        public string StatusDescription
        {
            get
            {
                return _statusDescription;
            }
            set
            {
                checkDisposedOrHeadersSent();
                if (value == null || value.Length == 0)
                {
                    _statusDescription = _statusCode.GetStatusDescription();
                    return;
                }
                if (!value.IsText() || value.IndexOfAny(new char[2]
                {
                    '\r',
                    '\n'
                }) > -1)
                {
                    throw new ArgumentException("Contains invalid characters.", "value");
                }
                _statusDescription = value;
            }
        }

        internal HttpListenerResponse(HttpListenerContext context)
        {
            _context = context;
            _keepAlive = true;
            _statusCode = 200;
            _statusDescription = "OK";
            _version = HttpVersion.Version11;
        }

        private bool canAddOrUpdate(Cookie cookie)
        {
            if (_cookies == null || _cookies.Count == 0)
            {
                return true;
            }
            List<Cookie> list = findCookie(cookie).ToList();
            if (list.Count == 0)
            {
                return true;
            }
            int version = cookie.Version;
            foreach (Cookie item in list)
            {
                if (item.Version == version)
                {
                    return true;
                }
            }
            return false;
        }

        private void checkDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        private void checkDisposedOrHeadersSent()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
            if (_headersSent)
            {
                throw new InvalidOperationException("Cannot be changed after the headers are sent.");
            }
        }

        private void close(bool force)
        {
            _disposed = true;
            _context.Connection.Close(force);
        }

        private IEnumerable<Cookie> findCookie(Cookie cookie)
        {
            string name = cookie.Name;
            string domain = cookie.Domain;
            string path = cookie.Path;
            if (_cookies == null)
            {
                yield break;
            }
            foreach (Cookie c in _cookies)
            {
                if (c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase) && c.Path.Equals(path, StringComparison.Ordinal))
                {
                    yield return c;
                }
            }
        }

        internal WebHeaderCollection WriteHeadersTo(MemoryStream destination)
        {
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection(HttpHeaderType.Response, internallyUsed: true);
            if (_headers != null)
            {
                webHeaderCollection.Add(_headers);
            }
            if (_contentType != null)
            {
                string value = (_contentType.IndexOf("charset=", StringComparison.Ordinal) == -1 && _contentEncoding != null) ? $"{_contentType}; charset={_contentEncoding.WebName}" : _contentType;
                webHeaderCollection.InternalSet("Content-Type", value, response: true);
            }
            if (webHeaderCollection["Server"] == null)
            {
                webHeaderCollection.InternalSet("Server", "websocket-sharp/1.0", response: true);
            }
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            if (webHeaderCollection["Date"] == null)
            {
                webHeaderCollection.InternalSet("Date", DateTime.UtcNow.ToString("r", invariantCulture), response: true);
            }
            if (!_sendChunked)
            {
                webHeaderCollection.InternalSet("Content-Length", _contentLength.ToString(invariantCulture), response: true);
            }
            else
            {
                webHeaderCollection.InternalSet("Transfer-Encoding", "chunked", response: true);
            }
            bool flag = !_context.Request.KeepAlive || !_keepAlive || _statusCode == 400 || _statusCode == 408 || _statusCode == 411 || _statusCode == 413 || _statusCode == 414 || _statusCode == 500 || _statusCode == 503;
            int reuses = _context.Connection.Reuses;
            if (flag || reuses >= 100)
            {
                webHeaderCollection.InternalSet("Connection", "close", response: true);
            }
            else
            {
                webHeaderCollection.InternalSet("Keep-Alive", $"timeout=15,max={100 - reuses}", response: true);
                if (_context.Request.ProtocolVersion < HttpVersion.Version11)
                {
                    webHeaderCollection.InternalSet("Connection", "keep-alive", response: true);
                }
            }
            if (_location != null)
            {
                webHeaderCollection.InternalSet("Location", _location, response: true);
            }
            if (_cookies != null)
            {
                foreach (Cookie cooky in _cookies)
                {
                    webHeaderCollection.InternalSet("Set-Cookie", cooky.ToResponseString(), response: true);
                }
            }
            Encoding encoding = _contentEncoding ?? Encoding.Default;
            StreamWriter streamWriter = new StreamWriter(destination, encoding, 256);
            streamWriter.Write("HTTP/{0} {1} {2}\r\n", _version, _statusCode, _statusDescription);
            streamWriter.Write(webHeaderCollection.ToStringMultiValue(response: true));
            streamWriter.Flush();
            destination.Position = encoding.GetPreamble().Length;
            return webHeaderCollection;
        }

        public void Abort()
        {
            if (!_disposed)
            {
                close(force: true);
            }
        }

        public void AddHeader(string name, string value)
        {
            Headers.Set(name, value);
        }

        public void AppendCookie(Cookie cookie)
        {
            Cookies.Add(cookie);
        }

        public void AppendHeader(string name, string value)
        {
            Headers.Add(name, value);
        }

        public void Close()
        {
            if (!_disposed)
            {
                close(force: false);
            }
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            checkDisposed();
            if (responseEntity == null)
            {
                throw new ArgumentNullException("responseEntity");
            }
            int count = responseEntity.Length;
            Stream output = OutputStream;
            if (willBlock)
            {
                output.Write(responseEntity, 0, count);
                close(force: false);
                return;
            }
            output.BeginWrite(responseEntity, 0, count, delegate (IAsyncResult ar)
            {
                output.EndWrite(ar);
                close(force: false);
            }, null);
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            if (templateResponse == null)
            {
                throw new ArgumentNullException("templateResponse");
            }
            if (templateResponse._headers != null)
            {
                if (_headers != null)
                {
                    _headers.Clear();
                }
                Headers.Add(templateResponse._headers);
            }
            else if (_headers != null)
            {
                _headers = null;
            }
            _contentLength = templateResponse._contentLength;
            _statusCode = templateResponse._statusCode;
            _statusDescription = templateResponse._statusDescription;
            _keepAlive = templateResponse._keepAlive;
            _version = templateResponse._version;
        }

        public void Redirect(string url)
        {
            checkDisposedOrHeadersSent();
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            Uri result = null;
            if (!url.MaybeUri() || !Uri.TryCreate(url, UriKind.Absolute, out result))
            {
                throw new ArgumentException("Not an absolute URL.", "url");
            }
            _location = url;
            _statusCode = 302;
            _statusDescription = "Found";
        }

        public void SetCookie(Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException("cookie");
            }
            if (!canAddOrUpdate(cookie))
            {
                throw new ArgumentException("Cannot be replaced.", "cookie");
            }
            Cookies.Add(cookie);
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
