using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp
{
	public class WebSocket : IDisposable
	{
		private AuthenticationChallenge _authChallenge;

		private string _base64Key;

		private bool _client;

		private Action _closeContext;

		private CompressionMethod _compression;

		private WebSocketContext _context;

		private CookieCollection _cookies;

		private NetworkCredential _credentials;

		private bool _emitOnPing;

		private bool _enableRedirection;

		private string _extensions;

		private bool _extensionsRequested;

		private object _forMessageEventQueue;

		private object _forPing;

		private object _forSend;

		private object _forState;

		private MemoryStream _fragmentsBuffer;

		private bool _fragmentsCompressed;

		private Opcode _fragmentsOpcode;

		private const string _guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

		private Func<WebSocketContext, string> _handshakeRequestChecker;

		private bool _ignoreExtensions;

		private bool _inContinuation;

		private volatile bool _inMessage;

		private volatile Logger _logger;

		private static readonly int _maxRetryCountForConnect;

		private Action<MessageEventArgs> _message;

		private Queue<MessageEventArgs> _messageEventQueue;

		private uint _nonceCount;

		private string _origin;

		private ManualResetEvent _pongReceived;

		private bool _preAuth;

		private string _protocol;

		private string[] _protocols;

		private bool _protocolsRequested;

		private NetworkCredential _proxyCredentials;

		private Uri _proxyUri;

		private volatile WebSocketState _readyState;

		private ManualResetEvent _receivingExited;

		private int _retryCountForConnect;

		private bool _secure;

		private ClientSslConfiguration _sslConfig;

		private Stream _stream;

		private TcpClient _tcpClient;

		private Uri _uri;

		private const string _version = "13";

		private TimeSpan _waitTime;

		internal static readonly byte[] EmptyBytes;

		internal static readonly int FragmentLength;

		internal static readonly RandomNumberGenerator RandomNumber;

		internal CookieCollection CookieCollection => _cookies;

		internal Func<WebSocketContext, string> CustomHandshakeRequestChecker
		{
			get
			{
				return _handshakeRequestChecker;
			}
			set
			{
				_handshakeRequestChecker = value;
			}
		}

		internal bool HasMessage
		{
			get
			{
				lock (_forMessageEventQueue)
				{
					return _messageEventQueue.Count > 0;
				}
			}
		}

		internal bool IgnoreExtensions
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

		internal bool IsConnected => _readyState == WebSocketState.Open || _readyState == WebSocketState.Closing;

		public CompressionMethod Compression
		{
			get
			{
				return _compression;
			}
			set
			{
				string message = null;
				if (!_client)
				{
					message = "This instance is not a client.";
					throw new InvalidOperationException(message);
				}
				if (!canSet(out message))
				{
					_logger.Warn(message);
					return;
				}
				lock (_forState)
				{
					if (!canSet(out message))
					{
						_logger.Warn(message);
					}
					else
					{
						_compression = value;
					}
				}
			}
		}

		public IEnumerable<Cookie> Cookies
		{
			get
			{
				lock (_cookies.SyncRoot)
				{
					foreach (Cookie cooky in _cookies)
					{
						yield return cooky;
					}
				}
			}
		}

		public NetworkCredential Credentials => _credentials;

		public bool EmitOnPing
		{
			get
			{
				return _emitOnPing;
			}
			set
			{
				_emitOnPing = value;
			}
		}

		public bool EnableRedirection
		{
			get
			{
				return _enableRedirection;
			}
			set
			{
				string message = null;
				if (!_client)
				{
					message = "This instance is not a client.";
					throw new InvalidOperationException(message);
				}
				if (!canSet(out message))
				{
					_logger.Warn(message);
					return;
				}
				lock (_forState)
				{
					if (!canSet(out message))
					{
						_logger.Warn(message);
					}
					else
					{
						_enableRedirection = value;
					}
				}
			}
		}

		public string Extensions => _extensions ?? string.Empty;

		public bool IsAlive => ping(EmptyBytes);

		public bool IsSecure => _secure;

		public Logger Log
		{
			get
			{
				return _logger;
			}
			internal set
			{
				_logger = value;
			}
		}

		public string Origin
		{
			get
			{
				return _origin;
			}
			set
			{
				string message = null;
				if (!_client)
				{
					message = "This instance is not a client.";
					throw new InvalidOperationException(message);
				}
				if (!value.IsNullOrEmpty())
				{
					if (!Uri.TryCreate(value, UriKind.Absolute, out Uri result))
					{
						message = "Not an absolute URI string.";
						throw new ArgumentException(message, "value");
					}
					if (result.Segments.Length > 1)
					{
						message = "It includes the path segments.";
						throw new ArgumentException(message, "value");
					}
				}
				if (!canSet(out message))
				{
					_logger.Warn(message);
					return;
				}
				lock (_forState)
				{
					if (!canSet(out message))
					{
						_logger.Warn(message);
						return;
					}
					_origin = ((!value.IsNullOrEmpty()) ? value.TrimEnd('/') : value);
				}
			}
		}

		public string Protocol
		{
			get
			{
				return _protocol ?? string.Empty;
			}
			internal set
			{
				_protocol = value;
			}
		}

		public WebSocketState ReadyState => _readyState;

		public ClientSslConfiguration SslConfiguration
		{
			get
			{
				if (!_client)
				{
					string message = "This instance is not a client.";
					throw new InvalidOperationException(message);
				}
				if (!_secure)
				{
					string message2 = "This instance does not use a secure connection.";
					throw new InvalidOperationException(message2);
				}
				return getSslConfiguration();
			}
		}

		public Uri Url => _client ? _uri : _context.RequestUri;

		public TimeSpan WaitTime
		{
			get
			{
				return _waitTime;
			}
			set
			{
				if (value <= TimeSpan.Zero)
				{
					throw new ArgumentOutOfRangeException("value", "Zero or less.");
				}
				if (!canSet(out string message))
				{
					_logger.Warn(message);
					return;
				}
				lock (_forState)
				{
					if (!canSet(out message))
					{
						_logger.Warn(message);
					}
					else
					{
						_waitTime = value;
					}
				}
			}
		}

		public event EventHandler<CloseEventArgs> OnClose;

		public event EventHandler<ErrorEventArgs> OnError;

		public event EventHandler<MessageEventArgs> OnMessage;

		public event EventHandler OnOpen;

		static WebSocket()
		{
			_maxRetryCountForConnect = 10;
			EmptyBytes = new byte[0];
			FragmentLength = 1016;
			RandomNumber = new RNGCryptoServiceProvider();
		}

		internal WebSocket(HttpListenerWebSocketContext context, string protocol)
		{
			_context = context;
			_protocol = protocol;
			_closeContext = context.Close;
			_logger = context.Log;
			_message = messages;
			_secure = context.IsSecureConnection;
			_stream = context.Stream;
			_waitTime = TimeSpan.FromSeconds(1.0);
			init();
		}

		internal WebSocket(TcpListenerWebSocketContext context, string protocol)
		{
			_context = context;
			_protocol = protocol;
			_closeContext = context.Close;
			_logger = context.Log;
			_message = messages;
			_secure = context.IsSecureConnection;
			_stream = context.Stream;
			_waitTime = TimeSpan.FromSeconds(1.0);
			init();
		}

		public WebSocket(string url, params string[] protocols)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			if (url.Length == 0)
			{
				throw new ArgumentException("An empty string.", "url");
			}
			if (!url.TryCreateWebSocketUri(out _uri, out string message))
			{
				throw new ArgumentException(message, "url");
			}
			if (protocols != null && protocols.Length != 0)
			{
				if (!checkProtocols(protocols, out message))
				{
					throw new ArgumentException(message, "protocols");
				}
				_protocols = protocols;
			}
			_base64Key = CreateBase64Key();
			_client = true;
			_logger = new Logger();
			_message = messagec;
			_secure = (_uri.Scheme == "wss");
			_waitTime = TimeSpan.FromSeconds(5.0);
			init();
		}

		private bool accept()
		{
			if (_readyState == WebSocketState.Open)
			{
				string message = "The handshake request has already been accepted.";
				_logger.Warn(message);
				return false;
			}
			lock (_forState)
			{
				if (_readyState == WebSocketState.Open)
				{
					string message2 = "The handshake request has already been accepted.";
					_logger.Warn(message2);
					return false;
				}
				if (_readyState == WebSocketState.Closing)
				{
					string message3 = "The close process has set in.";
					_logger.Error(message3);
					message3 = "An interruption has occurred while attempting to accept.";
					error(message3, null);
					return false;
				}
				if (_readyState == WebSocketState.Closed)
				{
					string message4 = "The connection has been closed.";
					_logger.Error(message4);
					message4 = "An interruption has occurred while attempting to accept.";
					error(message4, null);
					return false;
				}
				try
				{
					if (!acceptHandshake())
					{
						return false;
					}
				}
				catch (Exception ex)
				{
					_logger.Fatal(ex.Message);
					_logger.Debug(ex.ToString());
					string message5 = "An exception has occurred while attempting to accept.";
					fatal(message5, ex);
					return false;
				}
				_readyState = WebSocketState.Open;
				return true;
			}
		}

		private bool acceptHandshake()
		{
			_logger.Debug($"A handshake request from {_context.UserEndPoint}:\n{_context}");
			if (!checkHandshakeRequest(_context, out string message))
			{
				_logger.Error(message);
				refuseHandshake(CloseStatusCode.ProtocolError, "A handshake error has occurred while attempting to accept.");
				return false;
			}
			if (!customCheckHandshakeRequest(_context, out message))
			{
				_logger.Error(message);
				refuseHandshake(CloseStatusCode.PolicyViolation, "A handshake error has occurred while attempting to accept.");
				return false;
			}
			_base64Key = _context.Headers["Sec-WebSocket-Key"];
			if (_protocol != null)
			{
				IEnumerable<string> secWebSocketProtocols = _context.SecWebSocketProtocols;
				processSecWebSocketProtocolClientHeader(secWebSocketProtocols);
			}
			if (!_ignoreExtensions)
			{
				string value = _context.Headers["Sec-WebSocket-Extensions"];
				processSecWebSocketExtensionsClientHeader(value);
			}
			return sendHttpResponse(createHandshakeResponse());
		}

		private bool canSet(out string message)
		{
			message = null;
			if (_readyState == WebSocketState.Open)
			{
				message = "The connection has already been established.";
				return false;
			}
			if (_readyState == WebSocketState.Closing)
			{
				message = "The connection is closing.";
				return false;
			}
			return true;
		}

		private bool checkHandshakeRequest(WebSocketContext context, out string message)
		{
			message = null;
			if (!context.IsWebSocketRequest)
			{
				message = "Not a handshake request.";
				return false;
			}
			if (context.RequestUri == null)
			{
				message = "It specifies an invalid Request-URI.";
				return false;
			}
			NameValueCollection headers = context.Headers;
			string text = headers["Sec-WebSocket-Key"];
			if (text == null)
			{
				message = "It includes no Sec-WebSocket-Key header.";
				return false;
			}
			if (text.Length == 0)
			{
				message = "It includes an invalid Sec-WebSocket-Key header.";
				return false;
			}
			string text2 = headers["Sec-WebSocket-Version"];
			if (text2 == null)
			{
				message = "It includes no Sec-WebSocket-Version header.";
				return false;
			}
			if (text2 != "13")
			{
				message = "It includes an invalid Sec-WebSocket-Version header.";
				return false;
			}
			string text3 = headers["Sec-WebSocket-Protocol"];
			if (text3 != null && text3.Length == 0)
			{
				message = "It includes an invalid Sec-WebSocket-Protocol header.";
				return false;
			}
			if (!_ignoreExtensions)
			{
				string text4 = headers["Sec-WebSocket-Extensions"];
				if (text4 != null && text4.Length == 0)
				{
					message = "It includes an invalid Sec-WebSocket-Extensions header.";
					return false;
				}
			}
			return true;
		}

		private bool checkHandshakeResponse(HttpResponse response, out string message)
		{
			message = null;
			if (response.IsRedirect)
			{
				message = "Indicates the redirection.";
				return false;
			}
			if (response.IsUnauthorized)
			{
				message = "Requires the authentication.";
				return false;
			}
			if (!response.IsWebSocketResponse)
			{
				message = "Not a WebSocket handshake response.";
				return false;
			}
			NameValueCollection headers = response.Headers;
			if (!validateSecWebSocketAcceptHeader(headers["Sec-WebSocket-Accept"]))
			{
				message = "Includes no Sec-WebSocket-Accept header, or it has an invalid value.";
				return false;
			}
			if (!validateSecWebSocketProtocolServerHeader(headers["Sec-WebSocket-Protocol"]))
			{
				message = "Includes no Sec-WebSocket-Protocol header, or it has an invalid value.";
				return false;
			}
			if (!validateSecWebSocketExtensionsServerHeader(headers["Sec-WebSocket-Extensions"]))
			{
				message = "Includes an invalid Sec-WebSocket-Extensions header.";
				return false;
			}
			if (!validateSecWebSocketVersionServerHeader(headers["Sec-WebSocket-Version"]))
			{
				message = "Includes an invalid Sec-WebSocket-Version header.";
				return false;
			}
			return true;
		}

		private static bool checkProtocols(string[] protocols, out string message)
		{
			message = null;
			Func<string, bool> condition = (string protocol) => protocol.IsNullOrEmpty() || !protocol.IsToken();
			if (protocols.Contains(condition))
			{
				message = "It contains a value that is not a token.";
				return false;
			}
			if (protocols.ContainsTwice())
			{
				message = "It contains a value twice.";
				return false;
			}
			return true;
		}

		private bool checkReceivedFrame(WebSocketFrame frame, out string message)
		{
			message = null;
			bool isMasked = frame.IsMasked;
			if (_client && isMasked)
			{
				message = "A frame from the server is masked.";
				return false;
			}
			if (!_client && !isMasked)
			{
				message = "A frame from a client is not masked.";
				return false;
			}
			if (_inContinuation && frame.IsData)
			{
				message = "A data frame has been received while receiving continuation frames.";
				return false;
			}
			if (frame.IsCompressed && _compression == CompressionMethod.None)
			{
				message = "A compressed frame has been received without any agreement for it.";
				return false;
			}
			if (frame.Rsv2 == Rsv.On)
			{
				message = "The RSV2 of a frame is non-zero without any negotiation for it.";
				return false;
			}
			if (frame.Rsv3 == Rsv.On)
			{
				message = "The RSV3 of a frame is non-zero without any negotiation for it.";
				return false;
			}
			return true;
		}

		private void close(ushort code, string reason)
		{
			if (_readyState == WebSocketState.Closing)
			{
				_logger.Info("The closing is already in progress.");
				return;
			}
			if (_readyState == WebSocketState.Closed)
			{
				_logger.Info("The connection has already been closed.");
				return;
			}
			if (code == 1005)
			{
				close(PayloadData.Empty, send: true, receive: true, received: false);
				return;
			}
			bool flag = !code.IsReserved();
			close(new PayloadData(code, reason), flag, flag, received: false);
		}

		private void close(PayloadData payloadData, bool send, bool receive, bool received)
		{
			lock (_forState)
			{
				if (_readyState == WebSocketState.Closing)
				{
					_logger.Info("The closing is already in progress.");
					return;
				}
				if (_readyState == WebSocketState.Closed)
				{
					_logger.Info("The connection has already been closed.");
					return;
				}
				send = (send && _readyState == WebSocketState.Open);
				receive = (send && receive);
				_readyState = WebSocketState.Closing;
			}
			_logger.Trace("Begin closing the connection.");
			bool wasClean = closeHandshake(payloadData, send, receive, received);
			releaseResources();
			_logger.Trace("End closing the connection.");
			_readyState = WebSocketState.Closed;
			CloseEventArgs closeEventArgs = new CloseEventArgs(payloadData);
			closeEventArgs.WasClean = wasClean;
			try
			{
				this.OnClose.Emit(this, closeEventArgs);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.ToString());
				error("An error has occurred during the OnClose event.", ex);
			}
		}

		private void closeAsync(ushort code, string reason)
		{
			if (_readyState == WebSocketState.Closing)
			{
				_logger.Info("The closing is already in progress.");
				return;
			}
			if (_readyState == WebSocketState.Closed)
			{
				_logger.Info("The connection has already been closed.");
				return;
			}
			if (code == 1005)
			{
				closeAsync(PayloadData.Empty, send: true, receive: true, received: false);
				return;
			}
			bool flag = !code.IsReserved();
			closeAsync(new PayloadData(code, reason), flag, flag, received: false);
		}

		private void closeAsync(PayloadData payloadData, bool send, bool receive, bool received)
		{
			Action<PayloadData, bool, bool, bool> closer = close;
			closer.BeginInvoke(payloadData, send, receive, received, delegate(IAsyncResult ar)
			{
				closer.EndInvoke(ar);
			}, null);
		}

		private bool closeHandshake(byte[] frameAsBytes, bool receive, bool received)
		{
			bool flag = frameAsBytes != null && sendBytes(frameAsBytes);
			if (!received && flag && receive && _receivingExited != null)
			{
				received = _receivingExited.WaitOne(_waitTime);
			}
			bool flag2 = flag && received;
			_logger.Debug($"Was clean?: {flag2}\n  sent: {flag}\n  received: {received}");
			return flag2;
		}

		private bool closeHandshake(PayloadData payloadData, bool send, bool receive, bool received)
		{
			bool flag = false;
			if (send)
			{
				WebSocketFrame webSocketFrame = WebSocketFrame.CreateCloseFrame(payloadData, _client);
				flag = sendBytes(webSocketFrame.ToArray());
				if (_client)
				{
					webSocketFrame.Unmask();
				}
			}
			if (!received && flag && receive && _receivingExited != null)
			{
				received = _receivingExited.WaitOne(_waitTime);
			}
			bool flag2 = flag && received;
			_logger.Debug($"Was clean?: {flag2}\n  sent: {flag}\n  received: {received}");
			return flag2;
		}

		private bool connect()
		{
			if (_readyState == WebSocketState.Open)
			{
				string message = "The connection has already been established.";
				_logger.Warn(message);
				return false;
			}
			lock (_forState)
			{
				if (_readyState == WebSocketState.Open)
				{
					string message2 = "The connection has already been established.";
					_logger.Warn(message2);
					return false;
				}
				if (_readyState == WebSocketState.Closing)
				{
					string message3 = "The close process has set in.";
					_logger.Error(message3);
					message3 = "An interruption has occurred while attempting to connect.";
					error(message3, null);
					return false;
				}
				if (_retryCountForConnect > _maxRetryCountForConnect)
				{
					string message4 = "An opportunity for reconnecting has been lost.";
					_logger.Error(message4);
					message4 = "An interruption has occurred while attempting to connect.";
					error(message4, null);
					return false;
				}
				_readyState = WebSocketState.Connecting;
				try
				{
					doHandshake();
				}
				catch (Exception ex)
				{
					_retryCountForConnect++;
					_logger.Fatal(ex.Message);
					_logger.Debug(ex.ToString());
					string message5 = "An exception has occurred while attempting to connect.";
					fatal(message5, ex);
					return false;
				}
				_retryCountForConnect = 1;
				_readyState = WebSocketState.Open;
				return true;
			}
		}

		private string createExtensions()
		{
			StringBuilder stringBuilder = new StringBuilder(80);
			if (_compression != 0)
			{
				string arg = _compression.ToExtensionString("server_no_context_takeover", "client_no_context_takeover");
				stringBuilder.AppendFormat("{0}, ", arg);
			}
			int length = stringBuilder.Length;
			if (length > 2)
			{
				stringBuilder.Length = length - 2;
				return stringBuilder.ToString();
			}
			return null;
		}

		private HttpResponse createHandshakeFailureResponse(HttpStatusCode code)
		{
			HttpResponse httpResponse = HttpResponse.CreateCloseResponse(code);
			httpResponse.Headers["Sec-WebSocket-Version"] = "13";
			return httpResponse;
		}

		private HttpRequest createHandshakeRequest()
		{
			HttpRequest httpRequest = HttpRequest.CreateWebSocketRequest(_uri);
			NameValueCollection headers = httpRequest.Headers;
			if (!_origin.IsNullOrEmpty())
			{
				headers["Origin"] = _origin;
			}
			headers["Sec-WebSocket-Key"] = _base64Key;
			_protocolsRequested = (_protocols != null);
			if (_protocolsRequested)
			{
				headers["Sec-WebSocket-Protocol"] = _protocols.ToString(", ");
			}
			_extensionsRequested = (_compression != CompressionMethod.None);
			if (_extensionsRequested)
			{
				headers["Sec-WebSocket-Extensions"] = createExtensions();
			}
			headers["Sec-WebSocket-Version"] = "13";
			AuthenticationResponse authenticationResponse = null;
			if (_authChallenge != null && _credentials != null)
			{
				authenticationResponse = new AuthenticationResponse(_authChallenge, _credentials, _nonceCount);
				_nonceCount = authenticationResponse.NonceCount;
			}
			else if (_preAuth)
			{
				authenticationResponse = new AuthenticationResponse(_credentials);
			}
			if (authenticationResponse != null)
			{
				headers["Authorization"] = authenticationResponse.ToString();
			}
			if (_cookies.Count > 0)
			{
				httpRequest.SetCookies(_cookies);
			}
			return httpRequest;
		}

		private HttpResponse createHandshakeResponse()
		{
			HttpResponse httpResponse = HttpResponse.CreateWebSocketResponse();
			NameValueCollection headers = httpResponse.Headers;
			headers["Sec-WebSocket-Accept"] = CreateResponseKey(_base64Key);
			if (_protocol != null)
			{
				headers["Sec-WebSocket-Protocol"] = _protocol;
			}
			if (_extensions != null)
			{
				headers["Sec-WebSocket-Extensions"] = _extensions;
			}
			if (_cookies.Count > 0)
			{
				httpResponse.SetCookies(_cookies);
			}
			return httpResponse;
		}

		private bool customCheckHandshakeRequest(WebSocketContext context, out string message)
		{
			message = null;
			if (_handshakeRequestChecker == null)
			{
				return true;
			}
			message = _handshakeRequestChecker(context);
			return message == null;
		}

		private MessageEventArgs dequeueFromMessageEventQueue()
		{
			lock (_forMessageEventQueue)
			{
				return (_messageEventQueue.Count > 0) ? _messageEventQueue.Dequeue() : null;
			}
		}

		private void doHandshake()
		{
			setClientStream();
			HttpResponse httpResponse = sendHandshakeRequest();
			if (!checkHandshakeResponse(httpResponse, out string message))
			{
				throw new WebSocketException(CloseStatusCode.ProtocolError, message);
			}
			if (_protocolsRequested)
			{
				_protocol = httpResponse.Headers["Sec-WebSocket-Protocol"];
			}
			if (_extensionsRequested)
			{
				processSecWebSocketExtensionsServerHeader(httpResponse.Headers["Sec-WebSocket-Extensions"]);
			}
			processCookies(httpResponse.Cookies);
		}

		private void enqueueToMessageEventQueue(MessageEventArgs e)
		{
			lock (_forMessageEventQueue)
			{
				_messageEventQueue.Enqueue(e);
			}
		}

		private void error(string message, Exception exception)
		{
			try
			{
				this.OnError.Emit(this, new ErrorEventArgs(message, exception));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				_logger.Debug(ex.ToString());
			}
		}

		private void fatal(string message, Exception exception)
		{
			CloseStatusCode code = (exception is WebSocketException) ? ((WebSocketException)exception).Code : CloseStatusCode.Abnormal;
			fatal(message, (ushort)code);
		}

		private void fatal(string message, ushort code)
		{
			PayloadData payloadData = new PayloadData(code, message);
			close(payloadData, !code.IsReserved(), receive: false, received: false);
		}

		private void fatal(string message, CloseStatusCode code)
		{
			fatal(message, (ushort)code);
		}

		private ClientSslConfiguration getSslConfiguration()
		{
			if (_sslConfig == null)
			{
				_sslConfig = new ClientSslConfiguration(_uri.DnsSafeHost);
			}
			return _sslConfig;
		}

		private void init()
		{
			_compression = CompressionMethod.None;
			_cookies = new CookieCollection();
			_forPing = new object();
			_forSend = new object();
			_forState = new object();
			_messageEventQueue = new Queue<MessageEventArgs>();
			_forMessageEventQueue = ((ICollection)_messageEventQueue).SyncRoot;
			_readyState = WebSocketState.Connecting;
		}

		private void message()
		{
			MessageEventArgs obj = null;
			lock (_forMessageEventQueue)
			{
				if (_inMessage || _messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
				{
					return;
				}
				_inMessage = true;
				obj = _messageEventQueue.Dequeue();
			}
			_message(obj);
		}

		private void messagec(MessageEventArgs e)
		{
			while (true)
			{
				try
				{
					this.OnMessage.Emit(this, e);
				}
				catch (Exception ex)
				{
					_logger.Error(ex.ToString());
					error("An error has occurred during an OnMessage event.", ex);
				}
				lock (_forMessageEventQueue)
				{
					if (_messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
					{
						_inMessage = false;
						return;
					}
					e = _messageEventQueue.Dequeue();
				}
				bool flag = true;
			}
		}

		private void messages(MessageEventArgs e)
		{
			try
			{
				this.OnMessage.Emit(this, e);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.ToString());
				error("An error has occurred during an OnMessage event.", ex);
			}
			lock (_forMessageEventQueue)
			{
				if (_messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
				{
					_inMessage = false;
					return;
				}
				e = _messageEventQueue.Dequeue();
			}
			ThreadPool.QueueUserWorkItem(delegate
			{
				messages(e);
			});
		}

		private void open()
		{
			_inMessage = true;
			startReceiving();
			try
			{
				this.OnOpen.Emit(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.ToString());
				error("An error has occurred during the OnOpen event.", ex);
			}
			MessageEventArgs obj = null;
			lock (_forMessageEventQueue)
			{
				if (_messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
				{
					_inMessage = false;
					return;
				}
				obj = _messageEventQueue.Dequeue();
			}
			_message.BeginInvoke(obj, delegate(IAsyncResult ar)
			{
				_message.EndInvoke(ar);
			}, null);
		}

		private bool ping(byte[] data)
		{
			if (_readyState != WebSocketState.Open)
			{
				return false;
			}
			ManualResetEvent pongReceived = _pongReceived;
			if (pongReceived == null)
			{
				return false;
			}
			lock (_forPing)
			{
				try
				{
					pongReceived.Reset();
					if (!send(Fin.Final, Opcode.Ping, data, compressed: false))
					{
						return false;
					}
					return pongReceived.WaitOne(_waitTime);
				}
				catch (ObjectDisposedException)
				{
					return false;
				}
			}
		}

		private bool processCloseFrame(WebSocketFrame frame)
		{
			PayloadData payloadData = frame.PayloadData;
			close(payloadData, !payloadData.HasReservedCode, receive: false, received: true);
			return false;
		}

		private void processCookies(CookieCollection cookies)
		{
			if (cookies.Count != 0)
			{
				_cookies.SetOrRemove(cookies);
			}
		}

		private bool processDataFrame(WebSocketFrame frame)
		{
			enqueueToMessageEventQueue(frame.IsCompressed ? new MessageEventArgs(frame.Opcode, frame.PayloadData.ApplicationData.Decompress(_compression)) : new MessageEventArgs(frame));
			return true;
		}

		private bool processFragmentFrame(WebSocketFrame frame)
		{
			if (!_inContinuation)
			{
				if (frame.IsContinuation)
				{
					return true;
				}
				_fragmentsOpcode = frame.Opcode;
				_fragmentsCompressed = frame.IsCompressed;
				_fragmentsBuffer = new MemoryStream();
				_inContinuation = true;
			}
			_fragmentsBuffer.WriteBytes(frame.PayloadData.ApplicationData, 1024);
			if (frame.IsFinal)
			{
				using (_fragmentsBuffer)
				{
					byte[] rawData = _fragmentsCompressed ? _fragmentsBuffer.DecompressToArray(_compression) : _fragmentsBuffer.ToArray();
					enqueueToMessageEventQueue(new MessageEventArgs(_fragmentsOpcode, rawData));
				}
				_fragmentsBuffer = null;
				_inContinuation = false;
			}
			return true;
		}

		private bool processPingFrame(WebSocketFrame frame)
		{
			_logger.Trace("A ping was received.");
			WebSocketFrame webSocketFrame = WebSocketFrame.CreatePongFrame(frame.PayloadData, _client);
			lock (_forState)
			{
				if (_readyState != WebSocketState.Open)
				{
					_logger.Error("The connection is closing.");
					return true;
				}
				if (!sendBytes(webSocketFrame.ToArray()))
				{
					return false;
				}
			}
			_logger.Trace("A pong to this ping has been sent.");
			if (_emitOnPing)
			{
				if (_client)
				{
					webSocketFrame.Unmask();
				}
				enqueueToMessageEventQueue(new MessageEventArgs(frame));
			}
			return true;
		}

		private bool processPongFrame(WebSocketFrame frame)
		{
			_logger.Trace("A pong was received.");
			try
			{
				_pongReceived.Set();
			}
			catch (NullReferenceException ex)
			{
				_logger.Error(ex.Message);
				_logger.Debug(ex.ToString());
				return false;
			}
			catch (ObjectDisposedException ex2)
			{
				_logger.Error(ex2.Message);
				_logger.Debug(ex2.ToString());
				return false;
			}
			_logger.Trace("It has been signaled.");
			return true;
		}

		private bool processReceivedFrame(WebSocketFrame frame)
		{
			if (!checkReceivedFrame(frame, out string message))
			{
				throw new WebSocketException(CloseStatusCode.ProtocolError, message);
			}
			frame.Unmask();
			return frame.IsFragment ? processFragmentFrame(frame) : (frame.IsData ? processDataFrame(frame) : (frame.IsPing ? processPingFrame(frame) : (frame.IsPong ? processPongFrame(frame) : (frame.IsClose ? processCloseFrame(frame) : processUnsupportedFrame(frame)))));
		}

		private void processSecWebSocketExtensionsClientHeader(string value)
		{
			if (value == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(80);
			bool flag = false;
			foreach (string item in value.SplitHeaderValue(','))
			{
				string text = item.Trim();
				if (text.Length != 0 && !flag && text.IsCompressionExtension(CompressionMethod.Deflate))
				{
					_compression = CompressionMethod.Deflate;
					stringBuilder.AppendFormat("{0}, ", _compression.ToExtensionString("client_no_context_takeover", "server_no_context_takeover"));
					flag = true;
				}
			}
			int length = stringBuilder.Length;
			if (length > 2)
			{
				stringBuilder.Length = length - 2;
				_extensions = stringBuilder.ToString();
			}
		}

		private void processSecWebSocketExtensionsServerHeader(string value)
		{
			if (value == null)
			{
				_compression = CompressionMethod.None;
			}
			else
			{
				_extensions = value;
			}
		}

		private void processSecWebSocketProtocolClientHeader(IEnumerable<string> values)
		{
			if (!values.Contains((string val) => val == _protocol))
			{
				_protocol = null;
			}
		}

		private bool processUnsupportedFrame(WebSocketFrame frame)
		{
			_logger.Fatal("An unsupported frame:" + frame.PrintToString(dumped: false));
			fatal("There is no way to handle it.", CloseStatusCode.PolicyViolation);
			return false;
		}

		private void refuseHandshake(CloseStatusCode code, string reason)
		{
			_readyState = WebSocketState.Closing;
			HttpResponse response = createHandshakeFailureResponse(HttpStatusCode.BadRequest);
			sendHttpResponse(response);
			releaseServerResources();
			_readyState = WebSocketState.Closed;
			CloseEventArgs e = new CloseEventArgs(code, reason);
			try
			{
				this.OnClose.Emit(this, e);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				_logger.Debug(ex.ToString());
			}
		}

		private void releaseClientResources()
		{
			if (_stream != null)
			{
				_stream.Dispose();
				_stream = null;
			}
			if (_tcpClient != null)
			{
				_tcpClient.Close();
				_tcpClient = null;
			}
		}

		private void releaseCommonResources()
		{
			if (_fragmentsBuffer != null)
			{
				_fragmentsBuffer.Dispose();
				_fragmentsBuffer = null;
				_inContinuation = false;
			}
			if (_pongReceived != null)
			{
				_pongReceived.Close();
				_pongReceived = null;
			}
			if (_receivingExited != null)
			{
				_receivingExited.Close();
				_receivingExited = null;
			}
		}

		private void releaseResources()
		{
			if (_client)
			{
				releaseClientResources();
			}
			else
			{
				releaseServerResources();
			}
			releaseCommonResources();
		}

		private void releaseServerResources()
		{
			if (_closeContext != null)
			{
				_closeContext();
				_closeContext = null;
				_stream = null;
				_context = null;
			}
		}

		private bool send(Opcode opcode, Stream stream)
		{
			lock (_forSend)
			{
				Stream stream2 = stream;
				bool flag = false;
				bool flag2 = false;
				try
				{
					if (_compression != 0)
					{
						stream = stream.Compress(_compression);
						flag = true;
					}
					flag2 = send(opcode, stream, flag);
					if (!flag2)
					{
						error("A send has been interrupted.", null);
					}
				}
				catch (Exception ex)
				{
					_logger.Error(ex.ToString());
					error("An error has occurred during a send.", ex);
				}
				finally
				{
					if (flag)
					{
						stream.Dispose();
					}
					stream2.Dispose();
				}
				return flag2;
			}
		}

		private bool send(Opcode opcode, Stream stream, bool compressed)
		{
			long length = stream.Length;
			if (length == 0)
			{
				return send(Fin.Final, opcode, EmptyBytes, compressed: false);
			}
			long num = length / FragmentLength;
			int num2 = (int)(length % FragmentLength);
			byte[] array = null;
			int num3;
			switch (num)
			{
			case 0L:
				array = new byte[num2];
				return stream.Read(array, 0, num2) == num2 && send(Fin.Final, opcode, array, compressed);
			case 1L:
				num3 = ((num2 == 0) ? 1 : 0);
				break;
			default:
				num3 = 0;
				break;
			}
			if (num3 != 0)
			{
				array = new byte[FragmentLength];
				return stream.Read(array, 0, FragmentLength) == FragmentLength && send(Fin.Final, opcode, array, compressed);
			}
			array = new byte[FragmentLength];
			if (stream.Read(array, 0, FragmentLength) != FragmentLength || !send(Fin.More, opcode, array, compressed))
			{
				return false;
			}
			long num4 = (num2 == 0) ? (num - 2) : (num - 1);
			for (long num5 = 0L; num5 < num4; num5++)
			{
				if (stream.Read(array, 0, FragmentLength) != FragmentLength || !send(Fin.More, Opcode.Cont, array, compressed: false))
				{
					return false;
				}
			}
			if (num2 == 0)
			{
				num2 = FragmentLength;
			}
			else
			{
				array = new byte[num2];
			}
			return stream.Read(array, 0, num2) == num2 && send(Fin.Final, Opcode.Cont, array, compressed: false);
		}

		private bool send(Fin fin, Opcode opcode, byte[] data, bool compressed)
		{
			lock (_forState)
			{
				if (_readyState != WebSocketState.Open)
				{
					_logger.Error("The connection is closing.");
					return false;
				}
				WebSocketFrame webSocketFrame = new WebSocketFrame(fin, opcode, data, compressed, _client);
				return sendBytes(webSocketFrame.ToArray());
			}
		}

		private void sendAsync(Opcode opcode, Stream stream, Action<bool> completed)
		{
			Func<Opcode, Stream, bool> sender = send;
			sender.BeginInvoke(opcode, stream, delegate(IAsyncResult ar)
			{
				try
				{
					bool obj = sender.EndInvoke(ar);
					if (completed != null)
					{
						completed(obj);
					}
				}
				catch (Exception ex)
				{
					_logger.Error(ex.ToString());
					error("An error has occurred during the callback for an async send.", ex);
				}
			}, null);
		}

		private bool sendBytes(byte[] bytes)
		{
			try
			{
				_stream.Write(bytes, 0, bytes.Length);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				_logger.Debug(ex.ToString());
				return false;
			}
			return true;
		}

		private HttpResponse sendHandshakeRequest()
		{
			HttpRequest httpRequest = createHandshakeRequest();
			HttpResponse httpResponse = sendHttpRequest(httpRequest, 90000);
			if (httpResponse.IsUnauthorized)
			{
				string text = httpResponse.Headers["WWW-Authenticate"];
				_logger.Warn($"Received an authentication requirement for '{text}'.");
				if (text.IsNullOrEmpty())
				{
					_logger.Error("No authentication challenge is specified.");
					return httpResponse;
				}
				_authChallenge = AuthenticationChallenge.Parse(text);
				if (_authChallenge == null)
				{
					_logger.Error("An invalid authentication challenge is specified.");
					return httpResponse;
				}
				if (_credentials != null && (!_preAuth || _authChallenge.Scheme == AuthenticationSchemes.Digest))
				{
					if (httpResponse.HasConnectionClose)
					{
						releaseClientResources();
						setClientStream();
					}
					AuthenticationResponse authenticationResponse = new AuthenticationResponse(_authChallenge, _credentials, _nonceCount);
					_nonceCount = authenticationResponse.NonceCount;
					httpRequest.Headers["Authorization"] = authenticationResponse.ToString();
					httpResponse = sendHttpRequest(httpRequest, 15000);
				}
			}
			if (httpResponse.IsRedirect)
			{
				string text2 = httpResponse.Headers["Location"];
				_logger.Warn($"Received a redirection to '{text2}'.");
				if (_enableRedirection)
				{
					if (text2.IsNullOrEmpty())
					{
						_logger.Error("No url to redirect is located.");
						return httpResponse;
					}
					if (!text2.TryCreateWebSocketUri(out Uri result, out string message))
					{
						_logger.Error("An invalid url to redirect is located: " + message);
						return httpResponse;
					}
					releaseClientResources();
					_uri = result;
					_secure = (result.Scheme == "wss");
					setClientStream();
					return sendHandshakeRequest();
				}
			}
			return httpResponse;
		}

		private HttpResponse sendHttpRequest(HttpRequest request, int millisecondsTimeout)
		{
			_logger.Debug("A request to the server:\n" + request.ToString());
			HttpResponse response = request.GetResponse(_stream, millisecondsTimeout);
			_logger.Debug("A response to this request:\n" + response.ToString());
			return response;
		}

		private bool sendHttpResponse(HttpResponse response)
		{
			_logger.Debug($"A response to {_context.UserEndPoint}:\n{response}");
			return sendBytes(response.ToByteArray());
		}

		private void sendProxyConnectRequest()
		{
			HttpRequest httpRequest = HttpRequest.CreateConnectRequest(_uri);
			HttpResponse httpResponse = sendHttpRequest(httpRequest, 90000);
			if (httpResponse.IsProxyAuthenticationRequired)
			{
				string text = httpResponse.Headers["Proxy-Authenticate"];
				_logger.Warn($"Received a proxy authentication requirement for '{text}'.");
				if (text.IsNullOrEmpty())
				{
					throw new WebSocketException("No proxy authentication challenge is specified.");
				}
				AuthenticationChallenge authenticationChallenge = AuthenticationChallenge.Parse(text);
				if (authenticationChallenge == null)
				{
					throw new WebSocketException("An invalid proxy authentication challenge is specified.");
				}
				if (_proxyCredentials != null)
				{
					if (httpResponse.HasConnectionClose)
					{
						releaseClientResources();
						_tcpClient = new TcpClient(_proxyUri.DnsSafeHost, _proxyUri.Port);
						_stream = _tcpClient.GetStream();
					}
					AuthenticationResponse authenticationResponse = new AuthenticationResponse(authenticationChallenge, _proxyCredentials, 0u);
					httpRequest.Headers["Proxy-Authorization"] = authenticationResponse.ToString();
					httpResponse = sendHttpRequest(httpRequest, 15000);
				}
				if (httpResponse.IsProxyAuthenticationRequired)
				{
					throw new WebSocketException("A proxy authentication is required.");
				}
			}
			if (httpResponse.StatusCode[0] != '2')
			{
				throw new WebSocketException("The proxy has failed a connection to the requested host and port.");
			}
		}

		private void setClientStream()
		{
			if (_proxyUri != null)
			{
				_tcpClient = new TcpClient(_proxyUri.DnsSafeHost, _proxyUri.Port);
				_stream = _tcpClient.GetStream();
				sendProxyConnectRequest();
			}
			else
			{
				_tcpClient = new TcpClient(_uri.DnsSafeHost, _uri.Port);
				_stream = _tcpClient.GetStream();
			}
			if (_secure)
			{
				ClientSslConfiguration sslConfiguration = getSslConfiguration();
				string targetHost = sslConfiguration.TargetHost;
				if (targetHost != _uri.DnsSafeHost)
				{
					throw new WebSocketException(CloseStatusCode.TlsHandshakeFailure, "An invalid host name is specified.");
				}
				try
				{
					SslStream sslStream = new SslStream(_stream, leaveInnerStreamOpen: false, sslConfiguration.ServerCertificateValidationCallback, sslConfiguration.ClientCertificateSelectionCallback);
					sslStream.AuthenticateAsClient(targetHost);
					_stream = sslStream;
				}
				catch (Exception innerException)
				{
					throw new WebSocketException(CloseStatusCode.TlsHandshakeFailure, innerException);
				}
			}
		}

		private void startReceiving()
		{
			if (_messageEventQueue.Count > 0)
			{
				_messageEventQueue.Clear();
			}
			_pongReceived = new ManualResetEvent(initialState: false);
			_receivingExited = new ManualResetEvent(initialState: false);
			Action receive = null;
			receive = delegate
			{
				WebSocketFrame.ReadFrameAsync(_stream, unmask: false, delegate(WebSocketFrame frame)
				{
					if (!processReceivedFrame(frame) || _readyState == WebSocketState.Closed)
					{
						_receivingExited?.Set();
					}
					else
					{
						receive();
						if (!_inMessage && HasMessage && _readyState == WebSocketState.Open)
						{
							message();
						}
					}
				}, delegate(Exception ex)
				{
					_logger.Fatal(ex.ToString());
					fatal("An exception has occurred while receiving.", ex);
				});
			};
			receive();
		}

		private bool validateSecWebSocketAcceptHeader(string value)
		{
			return value != null && value == CreateResponseKey(_base64Key);
		}

		private bool validateSecWebSocketExtensionsServerHeader(string value)
		{
			if (value == null)
			{
				return true;
			}
			if (value.Length == 0)
			{
				return false;
			}
			if (!_extensionsRequested)
			{
				return false;
			}
			bool flag = _compression != CompressionMethod.None;
			foreach (string item in value.SplitHeaderValue(','))
			{
				string text = item.Trim();
				if (flag && text.IsCompressionExtension(_compression))
				{
					if (!text.Contains("server_no_context_takeover"))
					{
						_logger.Error("The server hasn't sent back 'server_no_context_takeover'.");
						return false;
					}
					if (!text.Contains("client_no_context_takeover"))
					{
						_logger.Warn("The server hasn't sent back 'client_no_context_takeover'.");
					}
					string method = _compression.ToExtensionString();
					if (text.SplitHeaderValue(';').Contains(delegate(string t)
					{
						t = t.Trim();
						return t != method && t != "server_no_context_takeover" && t != "client_no_context_takeover";
					}))
					{
						return false;
					}
					continue;
				}
				return false;
			}
			return true;
		}

		private bool validateSecWebSocketProtocolServerHeader(string value)
		{
			if (value == null)
			{
				return !_protocolsRequested;
			}
			if (value.Length == 0)
			{
				return false;
			}
			return _protocolsRequested && _protocols.Contains((string p) => p == value);
		}

		private bool validateSecWebSocketVersionServerHeader(string value)
		{
			return value == null || value == "13";
		}

		internal void Close(HttpResponse response)
		{
			_readyState = WebSocketState.Closing;
			sendHttpResponse(response);
			releaseServerResources();
			_readyState = WebSocketState.Closed;
		}

		internal void Close(HttpStatusCode code)
		{
			Close(createHandshakeFailureResponse(code));
		}

		internal void Close(PayloadData payloadData, byte[] frameAsBytes)
		{
			lock (_forState)
			{
				if (_readyState == WebSocketState.Closing)
				{
					_logger.Info("The closing is already in progress.");
					return;
				}
				if (_readyState == WebSocketState.Closed)
				{
					_logger.Info("The connection has already been closed.");
					return;
				}
				_readyState = WebSocketState.Closing;
			}
			_logger.Trace("Begin closing the connection.");
			bool flag = frameAsBytes != null && sendBytes(frameAsBytes);
			bool flag2 = flag && _receivingExited != null && _receivingExited.WaitOne(_waitTime);
			bool flag3 = flag && flag2;
			_logger.Debug($"Was clean?: {flag3}\n  sent: {flag}\n  received: {flag2}");
			releaseServerResources();
			releaseCommonResources();
			_logger.Trace("End closing the connection.");
			_readyState = WebSocketState.Closed;
			CloseEventArgs closeEventArgs = new CloseEventArgs(payloadData);
			closeEventArgs.WasClean = flag3;
			try
			{
				this.OnClose.Emit(this, closeEventArgs);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.ToString());
			}
		}

		internal static string CreateBase64Key()
		{
			byte[] array = new byte[16];
			RandomNumber.GetBytes(array);
			return Convert.ToBase64String(array);
		}

		internal static string CreateResponseKey(string base64Key)
		{
			StringBuilder stringBuilder = new StringBuilder(base64Key, 64);
			stringBuilder.Append("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
			SHA1 sHA = new SHA1CryptoServiceProvider();
			byte[] inArray = sHA.ComputeHash(stringBuilder.ToString().UTF8Encode());
			return Convert.ToBase64String(inArray);
		}

		internal void InternalAccept()
		{
			try
			{
				if (!acceptHandshake())
				{
					return;
				}
			}
			catch (Exception ex)
			{
				_logger.Fatal(ex.Message);
				_logger.Debug(ex.ToString());
				string message = "An exception has occurred while attempting to accept.";
				fatal(message, ex);
				return;
			}
			_readyState = WebSocketState.Open;
			open();
		}

		internal bool Ping(byte[] frameAsBytes, TimeSpan timeout)
		{
			if (_readyState != WebSocketState.Open)
			{
				return false;
			}
			ManualResetEvent pongReceived = _pongReceived;
			if (pongReceived == null)
			{
				return false;
			}
			lock (_forPing)
			{
				try
				{
					pongReceived.Reset();
					lock (_forState)
					{
						if (_readyState != WebSocketState.Open)
						{
							return false;
						}
						if (!sendBytes(frameAsBytes))
						{
							return false;
						}
					}
					return pongReceived.WaitOne(timeout);
				}
				catch (ObjectDisposedException)
				{
					return false;
				}
			}
		}

		internal void Send(Opcode opcode, byte[] data, Dictionary<CompressionMethod, byte[]> cache)
		{
			lock (_forSend)
			{
				lock (_forState)
				{
					if (_readyState != WebSocketState.Open)
					{
						_logger.Error("The connection is closing.");
						return;
					}
					if (!cache.TryGetValue(_compression, out byte[] value))
					{
						value = new WebSocketFrame(Fin.Final, opcode, data.Compress(_compression), _compression != CompressionMethod.None, mask: false).ToArray();
						cache.Add(_compression, value);
					}
					sendBytes(value);
				}
			}
		}

		internal void Send(Opcode opcode, Stream stream, Dictionary<CompressionMethod, Stream> cache)
		{
			lock (_forSend)
			{
				if (!cache.TryGetValue(_compression, out Stream value))
				{
					value = stream.Compress(_compression);
					cache.Add(_compression, value);
				}
				else
				{
					value.Position = 0L;
				}
				send(opcode, value, _compression != CompressionMethod.None);
			}
		}

		public void Accept()
		{
			if (_client)
			{
				string message = "This instance is a client.";
				throw new InvalidOperationException(message);
			}
			if (_readyState == WebSocketState.Closing)
			{
				string message2 = "The close process is in progress.";
				throw new InvalidOperationException(message2);
			}
			if (_readyState == WebSocketState.Closed)
			{
				string message3 = "The connection has already been closed.";
				throw new InvalidOperationException(message3);
			}
			if (accept())
			{
				open();
			}
		}

		public void AcceptAsync()
		{
			if (_client)
			{
				string message = "This instance is a client.";
				throw new InvalidOperationException(message);
			}
			if (_readyState == WebSocketState.Closing)
			{
				string message2 = "The close process is in progress.";
				throw new InvalidOperationException(message2);
			}
			if (_readyState == WebSocketState.Closed)
			{
				string message3 = "The connection has already been closed.";
				throw new InvalidOperationException(message3);
			}
			Func<bool> acceptor = accept;
			acceptor.BeginInvoke(delegate(IAsyncResult ar)
			{
				if (acceptor.EndInvoke(ar))
				{
					open();
				}
			}, null);
		}

		public void Close()
		{
			close(1005, string.Empty);
		}

		public void Close(ushort code)
		{
			if (!code.IsCloseStatusCode())
			{
				string message = "Less than 1000 or greater than 4999.";
				throw new ArgumentOutOfRangeException("code", message);
			}
			if (_client && code == 1011)
			{
				string message2 = "1011 cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			if (!_client && code == 1010)
			{
				string message3 = "1010 cannot be used.";
				throw new ArgumentException(message3, "code");
			}
			close(code, string.Empty);
		}

		public void Close(CloseStatusCode code)
		{
			if (_client && code == CloseStatusCode.ServerError)
			{
				string message = "ServerError cannot be used.";
				throw new ArgumentException(message, "code");
			}
			if (!_client && code == CloseStatusCode.MandatoryExtension)
			{
				string message2 = "MandatoryExtension cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			close((ushort)code, string.Empty);
		}

		public void Close(ushort code, string reason)
		{
			if (!code.IsCloseStatusCode())
			{
				string message = "Less than 1000 or greater than 4999.";
				throw new ArgumentOutOfRangeException("code", message);
			}
			if (_client && code == 1011)
			{
				string message2 = "1011 cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			if (!_client && code == 1010)
			{
				string message3 = "1010 cannot be used.";
				throw new ArgumentException(message3, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				close(code, string.Empty);
				return;
			}
			if (code == 1005)
			{
				string message4 = "1005 cannot be used.";
				throw new ArgumentException(message4, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message5 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message5, "reason");
			}
			if (bytes.Length > 123)
			{
				string message6 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", message6);
			}
			close(code, reason);
		}

		public void Close(CloseStatusCode code, string reason)
		{
			if (_client && code == CloseStatusCode.ServerError)
			{
				string message = "ServerError cannot be used.";
				throw new ArgumentException(message, "code");
			}
			if (!_client && code == CloseStatusCode.MandatoryExtension)
			{
				string message2 = "MandatoryExtension cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				close((ushort)code, string.Empty);
				return;
			}
			if (code == CloseStatusCode.NoStatus)
			{
				string message3 = "NoStatus cannot be used.";
				throw new ArgumentException(message3, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message4 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message4, "reason");
			}
			if (bytes.Length > 123)
			{
				string message5 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", message5);
			}
			close((ushort)code, reason);
		}

		public void CloseAsync()
		{
			closeAsync(1005, string.Empty);
		}

		public void CloseAsync(ushort code)
		{
			if (!code.IsCloseStatusCode())
			{
				string message = "Less than 1000 or greater than 4999.";
				throw new ArgumentOutOfRangeException("code", message);
			}
			if (_client && code == 1011)
			{
				string message2 = "1011 cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			if (!_client && code == 1010)
			{
				string message3 = "1010 cannot be used.";
				throw new ArgumentException(message3, "code");
			}
			closeAsync(code, string.Empty);
		}

		public void CloseAsync(CloseStatusCode code)
		{
			if (_client && code == CloseStatusCode.ServerError)
			{
				string message = "ServerError cannot be used.";
				throw new ArgumentException(message, "code");
			}
			if (!_client && code == CloseStatusCode.MandatoryExtension)
			{
				string message2 = "MandatoryExtension cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			closeAsync((ushort)code, string.Empty);
		}

		public void CloseAsync(ushort code, string reason)
		{
			if (!code.IsCloseStatusCode())
			{
				string message = "Less than 1000 or greater than 4999.";
				throw new ArgumentOutOfRangeException("code", message);
			}
			if (_client && code == 1011)
			{
				string message2 = "1011 cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			if (!_client && code == 1010)
			{
				string message3 = "1010 cannot be used.";
				throw new ArgumentException(message3, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				closeAsync(code, string.Empty);
				return;
			}
			if (code == 1005)
			{
				string message4 = "1005 cannot be used.";
				throw new ArgumentException(message4, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message5 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message5, "reason");
			}
			if (bytes.Length > 123)
			{
				string message6 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", message6);
			}
			closeAsync(code, reason);
		}

		public void CloseAsync(CloseStatusCode code, string reason)
		{
			if (_client && code == CloseStatusCode.ServerError)
			{
				string message = "ServerError cannot be used.";
				throw new ArgumentException(message, "code");
			}
			if (!_client && code == CloseStatusCode.MandatoryExtension)
			{
				string message2 = "MandatoryExtension cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				closeAsync((ushort)code, string.Empty);
				return;
			}
			if (code == CloseStatusCode.NoStatus)
			{
				string message3 = "NoStatus cannot be used.";
				throw new ArgumentException(message3, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message4 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message4, "reason");
			}
			if (bytes.Length > 123)
			{
				string message5 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", message5);
			}
			closeAsync((ushort)code, reason);
		}

		public void Connect()
		{
			if (!_client)
			{
				string message = "This instance is not a client.";
				throw new InvalidOperationException(message);
			}
			if (_readyState == WebSocketState.Closing)
			{
				string message2 = "The close process is in progress.";
				throw new InvalidOperationException(message2);
			}
			if (_retryCountForConnect > _maxRetryCountForConnect)
			{
				string message3 = "A series of reconnecting has failed.";
				throw new InvalidOperationException(message3);
			}
			if (connect())
			{
				open();
			}
		}

		public void ConnectAsync()
		{
			if (!_client)
			{
				string message = "This instance is not a client.";
				throw new InvalidOperationException(message);
			}
			if (_readyState == WebSocketState.Closing)
			{
				string message2 = "The close process is in progress.";
				throw new InvalidOperationException(message2);
			}
			if (_retryCountForConnect > _maxRetryCountForConnect)
			{
				string message3 = "A series of reconnecting has failed.";
				throw new InvalidOperationException(message3);
			}
			Func<bool> connector = connect;
			connector.BeginInvoke(delegate(IAsyncResult ar)
			{
				if (connector.EndInvoke(ar))
				{
					open();
				}
			}, null);
		}

		public bool Ping()
		{
			return ping(EmptyBytes);
		}

		public bool Ping(string message)
		{
			if (message.IsNullOrEmpty())
			{
				return ping(EmptyBytes);
			}
			if (!message.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message2 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message2, "message");
			}
			if (bytes.Length > 125)
			{
				string message3 = "Its size is greater than 125 bytes.";
				throw new ArgumentOutOfRangeException("message", message3);
			}
			return ping(bytes);
		}

		public void Send(byte[] data)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			send(Opcode.Binary, new MemoryStream(data));
		}

		public void Send(FileInfo fileInfo)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (fileInfo == null)
			{
				throw new ArgumentNullException("fileInfo");
			}
			if (!fileInfo.Exists)
			{
				string message2 = "The file does not exist.";
				throw new ArgumentException(message2, "fileInfo");
			}
			if (!fileInfo.TryOpenRead(out FileStream fileStream))
			{
				string message3 = "The file could not be opened.";
				throw new ArgumentException(message3, "fileInfo");
			}
			send(Opcode.Binary, fileStream);
		}

		public void Send(string data)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (!data.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message2 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message2, "data");
			}
			send(Opcode.Text, new MemoryStream(bytes));
		}

		public void Send(Stream stream, int length)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				string message2 = "It cannot be read.";
				throw new ArgumentException(message2, "stream");
			}
			if (length < 1)
			{
				string message3 = "Less than 1.";
				throw new ArgumentException(message3, "length");
			}
			byte[] array = stream.ReadBytes(length);
			int num = array.Length;
			if (num == 0)
			{
				string message4 = "No data could be read from it.";
				throw new ArgumentException(message4, "stream");
			}
			if (num < length)
			{
				_logger.Warn($"Only {num} byte(s) of data could be read from the stream.");
			}
			send(Opcode.Binary, new MemoryStream(array));
		}

		public void SendAsync(byte[] data, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			sendAsync(Opcode.Binary, new MemoryStream(data), completed);
		}

		public void SendAsync(FileInfo fileInfo, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (fileInfo == null)
			{
				throw new ArgumentNullException("fileInfo");
			}
			if (!fileInfo.Exists)
			{
				string message2 = "The file does not exist.";
				throw new ArgumentException(message2, "fileInfo");
			}
			if (!fileInfo.TryOpenRead(out FileStream fileStream))
			{
				string message3 = "The file could not be opened.";
				throw new ArgumentException(message3, "fileInfo");
			}
			sendAsync(Opcode.Binary, fileStream, completed);
		}

		public void SendAsync(string data, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (!data.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message2 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message2, "data");
			}
			sendAsync(Opcode.Text, new MemoryStream(bytes), completed);
		}

		public void SendAsync(Stream stream, int length, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string message = "The current state of the connection is not Open.";
				throw new InvalidOperationException(message);
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				string message2 = "It cannot be read.";
				throw new ArgumentException(message2, "stream");
			}
			if (length < 1)
			{
				string message3 = "Less than 1.";
				throw new ArgumentException(message3, "length");
			}
			byte[] array = stream.ReadBytes(length);
			int num = array.Length;
			if (num == 0)
			{
				string message4 = "No data could be read from it.";
				throw new ArgumentException(message4, "stream");
			}
			if (num < length)
			{
				_logger.Warn($"Only {num} byte(s) of data could be read from the stream.");
			}
			sendAsync(Opcode.Binary, new MemoryStream(array), completed);
		}

		public void SetCookie(Cookie cookie)
		{
			string message = null;
			if (!_client)
			{
				message = "This instance is not a client.";
				throw new InvalidOperationException(message);
			}
			if (cookie == null)
			{
				throw new ArgumentNullException("cookie");
			}
			if (!canSet(out message))
			{
				_logger.Warn(message);
				return;
			}
			lock (_forState)
			{
				if (!canSet(out message))
				{
					_logger.Warn(message);
					return;
				}
				lock (_cookies.SyncRoot)
				{
					_cookies.SetOrRemove(cookie);
				}
			}
		}

		public void SetCredentials(string username, string password, bool preAuth)
		{
			string message = null;
			if (!_client)
			{
				message = "This instance is not a client.";
				throw new InvalidOperationException(message);
			}
			if (!username.IsNullOrEmpty() && (username.Contains(':') || !username.IsText()))
			{
				message = "It contains an invalid character.";
				throw new ArgumentException(message, "username");
			}
			if (!password.IsNullOrEmpty() && !password.IsText())
			{
				message = "It contains an invalid character.";
				throw new ArgumentException(message, "password");
			}
			if (!canSet(out message))
			{
				_logger.Warn(message);
				return;
			}
			lock (_forState)
			{
				if (!canSet(out message))
				{
					_logger.Warn(message);
				}
				else if (username.IsNullOrEmpty())
				{
					_credentials = null;
					_preAuth = false;
				}
				else
				{
					_credentials = new NetworkCredential(username, password, _uri.PathAndQuery);
					_preAuth = preAuth;
				}
			}
		}

		public void SetProxy(string url, string username, string password)
		{
			string message = null;
			if (!_client)
			{
				message = "This instance is not a client.";
				throw new InvalidOperationException(message);
			}
			Uri result = null;
			if (!url.IsNullOrEmpty())
			{
				if (!Uri.TryCreate(url, UriKind.Absolute, out result))
				{
					message = "Not an absolute URI string.";
					throw new ArgumentException(message, "url");
				}
				if (result.Scheme != "http")
				{
					message = "The scheme part is not http.";
					throw new ArgumentException(message, "url");
				}
				if (result.Segments.Length > 1)
				{
					message = "It includes the path segments.";
					throw new ArgumentException(message, "url");
				}
			}
			if (!username.IsNullOrEmpty() && (username.Contains(':') || !username.IsText()))
			{
				message = "It contains an invalid character.";
				throw new ArgumentException(message, "username");
			}
			if (!password.IsNullOrEmpty() && !password.IsText())
			{
				message = "It contains an invalid character.";
				throw new ArgumentException(message, "password");
			}
			if (!canSet(out message))
			{
				_logger.Warn(message);
				return;
			}
			lock (_forState)
			{
				if (!canSet(out message))
				{
					_logger.Warn(message);
				}
				else if (url.IsNullOrEmpty())
				{
					_proxyUri = null;
					_proxyCredentials = null;
				}
				else
				{
					_proxyUri = result;
					_proxyCredentials = ((!username.IsNullOrEmpty()) ? new NetworkCredential(username, password, $"{_uri.DnsSafeHost}:{_uri.Port}") : null);
				}
			}
		}

		void IDisposable.Dispose()
		{
			close(1001, string.Empty);
		}
	}
}
