using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using WebSocketSharp.Net;

namespace WebSocketSharp
{
    internal class HttpRequest : HttpBase
    {
        private CookieCollection _cookies;

        private string _method;

        private string _uri;

        public AuthenticationResponse AuthenticationResponse
        {
            get
            {
                string text = base.Headers["Authorization"];
                return (text != null && text.Length > 0) ? AuthenticationResponse.Parse(text) : null;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                {
                    _cookies = base.Headers.GetCookies(response: false);
                }
                return _cookies;
            }
        }

        public string HttpMethod => _method;

        public bool IsWebSocketRequest => _method == "GET" && base.ProtocolVersion > HttpVersion.Version10 && base.Headers.Upgrades("websocket");

        public string RequestUri => _uri;

        private HttpRequest(string method, string uri, Version version, NameValueCollection headers)
            : base(version, headers)
        {
            _method = method;
            _uri = uri;
        }

        internal HttpRequest(string method, string uri)
            : this(method, uri, HttpVersion.Version11, new NameValueCollection())
        {
            base.Headers["User-Agent"] = "websocket-sharp/1.0";
        }

        internal static HttpRequest CreateConnectRequest(Uri uri)
        {
            string dnsSafeHost = uri.DnsSafeHost;
            int port = uri.Port;
            string text = $"{dnsSafeHost}:{port}";
            HttpRequest httpRequest = new HttpRequest("CONNECT", text);
            httpRequest.Headers["Host"] = ((port == 80) ? dnsSafeHost : text);
            return httpRequest;
        }

        internal static HttpRequest CreateWebSocketRequest(Uri uri)
        {
            HttpRequest httpRequest = new HttpRequest("GET", uri.PathAndQuery);
            NameValueCollection headers = httpRequest.Headers;
            int port = uri.Port;
            string scheme = uri.Scheme;
            headers["Host"] = (((port == 80 && scheme == "ws") || (port == 443 && scheme == "wss")) ? uri.DnsSafeHost : uri.Authority);
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";
            return httpRequest;
        }

        internal HttpResponse GetResponse(Stream stream, int millisecondsTimeout)
        {
            byte[] array = ToByteArray();
            stream.Write(array, 0, array.Length);
            return HttpBase.Read(stream, HttpResponse.Parse, millisecondsTimeout);
        }

        internal static HttpRequest Parse(string[] headerParts)
        {
            string[] array = headerParts[0].Split(new char[1]
            {
                ' '
            }, 3);
            if (array.Length != 3)
            {
                throw new ArgumentException("Invalid request line: " + headerParts[0]);
            }
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
            for (int i = 1; i < headerParts.Length; i++)
            {
                webHeaderCollection.InternalSet(headerParts[i], response: false);
            }
            return new HttpRequest(array[0], array[1], new Version(array[2].Substring(5)), webHeaderCollection);
        }

        internal static HttpRequest Read(Stream stream, int millisecondsTimeout)
        {
            return HttpBase.Read(stream, Parse, millisecondsTimeout);
        }

        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
            {
                return;
            }
            StringBuilder stringBuilder = new StringBuilder(64);
            foreach (Cookie item in cookies.Sorted)
            {
                if (!item.Expired)
                {
                    stringBuilder.AppendFormat("{0}; ", item.ToString());
                }
            }
            int length = stringBuilder.Length;
            if (length > 2)
            {
                stringBuilder.Length = length - 2;
                base.Headers["Cookie"] = stringBuilder.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(64);
            stringBuilder.AppendFormat("{0} {1} HTTP/{2}{3}", _method, _uri, base.ProtocolVersion, "\r\n");
            NameValueCollection headers = base.Headers;
            string[] allKeys = headers.AllKeys;
            foreach (string text in allKeys)
            {
                stringBuilder.AppendFormat("{0}: {1}{2}", text, headers[text], "\r\n");
            }
            stringBuilder.Append("\r\n");
            string entityBody = base.EntityBody;
            if (entityBody.Length > 0)
            {
                stringBuilder.Append(entityBody);
            }
            return stringBuilder.ToString();
        }
    }
}
