using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WebSocketSharp.Net
{
    public sealed class HttpListenerRequest
    {
        private static readonly byte[] _100continue;

        private string[] _acceptTypes;

        private bool _chunked;

        private HttpConnection _connection;

        private Encoding _contentEncoding;

        private long _contentLength;

        private HttpListenerContext _context;

        private CookieCollection _cookies;

        private WebHeaderCollection _headers;

        private string _httpMethod;

        private Stream _inputStream;

        private Version _protocolVersion;

        private NameValueCollection _queryString;

        private string _rawUrl;

        private Guid _requestTraceIdentifier;

        private Uri _url;

        private Uri _urlReferrer;

        private bool _urlSet;

        private string _userHostName;

        private string[] _userLanguages;

        public string[] AcceptTypes
        {
            get
            {
                string text = _headers["Accept"];
                if (text == null)
                {
                    return null;
                }
                if (_acceptTypes == null)
                {
                    _acceptTypes = text.SplitHeaderValue(',').Trim().ToList()
                        .ToArray();
                }
                return _acceptTypes;
            }
        }

        public int ClientCertificateError
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                {
                    _contentEncoding = (getContentEncoding() ?? Encoding.UTF8);
                }
                return _contentEncoding;
            }
        }

        public long ContentLength64 => _contentLength;

        public string ContentType => _headers["Content-Type"];

        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                {
                    _cookies = _headers.GetCookies(response: false);
                }
                return _cookies;
            }
        }

        public bool HasEntityBody => _contentLength > 0 || _chunked;

        public NameValueCollection Headers => _headers;

        public string HttpMethod => _httpMethod;

        public Stream InputStream
        {
            get
            {
                if (_inputStream == null)
                {
                    _inputStream = (getInputStream() ?? Stream.Null);
                }
                return _inputStream;
            }
        }

        public bool IsAuthenticated => _context.User != null;

        public bool IsLocal => _connection.IsLocal;

        public bool IsSecureConnection => _connection.IsSecure;

        public bool IsWebSocketRequest => _httpMethod == "GET" && _protocolVersion > HttpVersion.Version10 && _headers.Upgrades("websocket");

        public bool KeepAlive => _headers.KeepsAlive(_protocolVersion);

        public IPEndPoint LocalEndPoint => _connection.LocalEndPoint;

        public Version ProtocolVersion => _protocolVersion;

        public NameValueCollection QueryString
        {
            get
            {
                if (_queryString == null)
                {
                    Uri url = Url;
                    _queryString = QueryStringCollection.Parse((url != null) ? url.Query : null, Encoding.UTF8);
                }
                return _queryString;
            }
        }

        public string RawUrl => _rawUrl;

        public IPEndPoint RemoteEndPoint => _connection.RemoteEndPoint;

        public Guid RequestTraceIdentifier => _requestTraceIdentifier;

        public Uri Url
        {
            get
            {
                if (!_urlSet)
                {
                    _url = HttpUtility.CreateRequestUrl(_rawUrl, _userHostName ?? UserHostAddress, IsWebSocketRequest, IsSecureConnection);
                    _urlSet = true;
                }
                return _url;
            }
        }

        public Uri UrlReferrer
        {
            get
            {
                string text = _headers["Referer"];
                if (text == null)
                {
                    return null;
                }
                if (_urlReferrer == null)
                {
                    _urlReferrer = text.ToUri();
                }
                return _urlReferrer;
            }
        }

        public string UserAgent => _headers["User-Agent"];

        public string UserHostAddress => _connection.LocalEndPoint.ToString();

        public string UserHostName => _userHostName;

        public string[] UserLanguages
        {
            get
            {
                string text = _headers["Accept-Language"];
                if (text == null)
                {
                    return null;
                }
                if (_userLanguages == null)
                {
                    _userLanguages = text.Split(',').Trim().ToList()
                        .ToArray();
                }
                return _userLanguages;
            }
        }

        static HttpListenerRequest()
        {
            _100continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
        }

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            _connection = context.Connection;
            _contentLength = -1L;
            _headers = new WebHeaderCollection();
            _requestTraceIdentifier = Guid.NewGuid();
        }

        private void finishInitialization10()
        {
            string text = _headers["Transfer-Encoding"];
            if (text != null)
            {
                _context.ErrorMessage = "Invalid Transfer-Encoding header";
            }
            else if (_httpMethod == "POST")
            {
                if (_contentLength == -1)
                {
                    _context.ErrorMessage = "Content-Length header required";
                }
                else if (_contentLength == 0)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                }
            }
        }

        private Encoding getContentEncoding()
        {
            string text = _headers["Content-Type"];
            if (text == null)
            {
                return null;
            }
            HttpUtility.TryGetEncoding(text, out Encoding result);
            return result;
        }

        private RequestStream getInputStream()
        {
            return (_contentLength > 0 || _chunked) ? _connection.GetRequestStream(_contentLength, _chunked) : null;
        }

        internal void AddHeader(string headerField)
        {
            char c = headerField[0];
            if (c == ' ' || c == '\t')
            {
                _context.ErrorMessage = "Invalid header field";
                return;
            }
            int num = headerField.IndexOf(':');
            if (num < 1)
            {
                _context.ErrorMessage = "Invalid header field";
                return;
            }
            string text = headerField.Substring(0, num).Trim();
            if (text.Length == 0 || !text.IsToken())
            {
                _context.ErrorMessage = "Invalid header name";
                return;
            }
            string text2 = (num < headerField.Length - 1) ? headerField.Substring(num + 1).Trim() : string.Empty;
            _headers.InternalSet(text, text2, response: false);
            string a = text.ToLower(CultureInfo.InvariantCulture);
            if (a == "host")
            {
                if (_userHostName != null)
                {
                    _context.ErrorMessage = "Invalid Host header";
                }
                else if (text2.Length == 0)
                {
                    _context.ErrorMessage = "Invalid Host header";
                }
                else
                {
                    _userHostName = text2;
                }
            }
            else if (a == "content-length")
            {
                long result;
                if (_contentLength > -1)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                }
                else if (!long.TryParse(text2, out result))
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                }
                else if (result < 0)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                }
                else
                {
                    _contentLength = result;
                }
            }
        }

        internal void FinishInitialization()
        {
            if (_protocolVersion == HttpVersion.Version10)
            {
                finishInitialization10();
                return;
            }
            if (_userHostName == null)
            {
                _context.ErrorMessage = "Host header required";
                return;
            }
            string text = _headers["Transfer-Encoding"];
            if (text != null)
            {
                StringComparison comparisonType = StringComparison.OrdinalIgnoreCase;
                if (!text.Equals("chunked", comparisonType))
                {
                    _context.ErrorMessage = string.Empty;
                    _context.ErrorStatus = 501;
                    return;
                }
                _chunked = true;
            }
            if ((_httpMethod == "POST" || _httpMethod == "PUT") && _contentLength <= 0 && !_chunked)
            {
                _context.ErrorMessage = string.Empty;
                _context.ErrorStatus = 411;
                return;
            }
            string text2 = _headers["Expect"];
            if (text2 != null)
            {
                StringComparison comparisonType2 = StringComparison.OrdinalIgnoreCase;
                if (!text2.Equals("100-continue", comparisonType2))
                {
                    _context.ErrorMessage = "Invalid Expect header";
                    return;
                }
                ResponseStream responseStream = _connection.GetResponseStream();
                responseStream.InternalWrite(_100continue, 0, _100continue.Length);
            }
        }

        internal bool FlushInput()
        {
            Stream inputStream = InputStream;
            if (inputStream == Stream.Null)
            {
                return true;
            }
            int num = 2048;
            if (_contentLength > 0 && _contentLength < num)
            {
                num = (int)_contentLength;
            }
            byte[] buffer = new byte[num];
            while (true)
            {
                try
                {
                    IAsyncResult asyncResult = inputStream.BeginRead(buffer, 0, num, null, null);
                    if (!asyncResult.IsCompleted)
                    {
                        int millisecondsTimeout = 100;
                        if (!asyncResult.AsyncWaitHandle.WaitOne(millisecondsTimeout))
                        {
                            return false;
                        }
                    }
                    if (inputStream.EndRead(asyncResult) <= 0)
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        internal bool IsUpgradeRequest(string protocol)
        {
            return _headers.Upgrades(protocol);
        }

        internal void SetRequestLine(string requestLine)
        {
            string[] array = requestLine.Split(new char[1]
            {
                ' '
            }, 3);
            if (array.Length < 3)
            {
                _context.ErrorMessage = "Invalid request line (parts)";
                return;
            }
            string text = array[0];
            if (text.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (method)";
                return;
            }
            string text2 = array[1];
            if (text2.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (target)";
                return;
            }
            string text3 = array[2];
            if (text3.Length != 8)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }
            if (text3.IndexOf("HTTP/") != 0)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }
            if (!text3.Substring(5).TryCreateVersion(out Version result))
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }
            if (result.Major < 1)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }
            if (!text.IsHttpMethod(result))
            {
                _context.ErrorMessage = "Invalid request line (method)";
                return;
            }
            _httpMethod = text;
            _rawUrl = text2;
            _protocolVersion = result;
        }

        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            throw new NotSupportedException();
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        public X509Certificate2 GetClientCertificate()
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(64);
            stringBuilder.AppendFormat("{0} {1} HTTP/{2}\r\n", _httpMethod, _rawUrl, _protocolVersion).Append(_headers.ToString());
            return stringBuilder.ToString();
        }
    }
}
