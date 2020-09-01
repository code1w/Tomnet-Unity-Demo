using System;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp.Server
{
	public class HttpServer
	{
		private IPAddress _address;

		private string _docRootPath;

		private string _hostname;

		private WebSocketSharp.Net.HttpListener _listener;

		private Logger _log;

		private int _port;

		private Thread _receiveThread;

		private bool _secure;

		private WebSocketServiceManager _services;

		private volatile ServerState _state;

		private object _sync;

		public IPAddress Address => _address;

		public WebSocketSharp.Net.AuthenticationSchemes AuthenticationSchemes
		{
			get
			{
				return _listener.AuthenticationSchemes;
			}
			set
			{
				if (!canSet(out string message))
				{
					_log.Warn(message);
					return;
				}
				lock (_sync)
				{
					if (!canSet(out message))
					{
						_log.Warn(message);
					}
					else
					{
						_listener.AuthenticationSchemes = value;
					}
				}
			}
		}

		public string DocumentRootPath
		{
			get
			{
				return _docRootPath;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value.Length == 0)
				{
					throw new ArgumentException("An empty string.", "value");
				}
				value = value.TrimSlashOrBackslashFromEnd();
				string text = null;
				try
				{
					text = Path.GetFullPath(value);
				}
				catch (Exception innerException)
				{
					throw new ArgumentException("An invalid path string.", "value", innerException);
				}
				if (value == "/")
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				if (value == "\\")
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				if (value.Length == 2 && value[1] == ':')
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				if (text == "/")
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				text = text.TrimSlashOrBackslashFromEnd();
				if (text.Length == 2 && text[1] == ':')
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				if (!canSet(out string message))
				{
					_log.Warn(message);
					return;
				}
				lock (_sync)
				{
					if (!canSet(out message))
					{
						_log.Warn(message);
					}
					else
					{
						_docRootPath = value;
					}
				}
			}
		}

		public bool IsListening => _state == ServerState.Start;

		public bool IsSecure => _secure;

		public bool KeepClean
		{
			get
			{
				return _services.KeepClean;
			}
			set
			{
				_services.KeepClean = value;
			}
		}

		public Logger Log => _log;

		public int Port => _port;

		public string Realm
		{
			get
			{
				return _listener.Realm;
			}
			set
			{
				if (!canSet(out string message))
				{
					_log.Warn(message);
					return;
				}
				lock (_sync)
				{
					if (!canSet(out message))
					{
						_log.Warn(message);
					}
					else
					{
						_listener.Realm = value;
					}
				}
			}
		}

		public bool ReuseAddress
		{
			get
			{
				return _listener.ReuseAddress;
			}
			set
			{
				if (!canSet(out string message))
				{
					_log.Warn(message);
					return;
				}
				lock (_sync)
				{
					if (!canSet(out message))
					{
						_log.Warn(message);
					}
					else
					{
						_listener.ReuseAddress = value;
					}
				}
			}
		}

		public ServerSslConfiguration SslConfiguration
		{
			get
			{
				if (!_secure)
				{
					string message = "This instance does not provide secure connections.";
					throw new InvalidOperationException(message);
				}
				return _listener.SslConfiguration;
			}
		}

		public Func<IIdentity, WebSocketSharp.Net.NetworkCredential> UserCredentialsFinder
		{
			get
			{
				return _listener.UserCredentialsFinder;
			}
			set
			{
				if (!canSet(out string message))
				{
					_log.Warn(message);
					return;
				}
				lock (_sync)
				{
					if (!canSet(out message))
					{
						_log.Warn(message);
					}
					else
					{
						_listener.UserCredentialsFinder = value;
					}
				}
			}
		}

		public TimeSpan WaitTime
		{
			get
			{
				return _services.WaitTime;
			}
			set
			{
				_services.WaitTime = value;
			}
		}

		public WebSocketServiceManager WebSocketServices => _services;

		public event EventHandler<HttpRequestEventArgs> OnConnect;

		public event EventHandler<HttpRequestEventArgs> OnDelete;

		public event EventHandler<HttpRequestEventArgs> OnGet;

		public event EventHandler<HttpRequestEventArgs> OnHead;

		public event EventHandler<HttpRequestEventArgs> OnOptions;

		public event EventHandler<HttpRequestEventArgs> OnPost;

		public event EventHandler<HttpRequestEventArgs> OnPut;

		public event EventHandler<HttpRequestEventArgs> OnTrace;

		public HttpServer()
		{
			init("*", IPAddress.Any, 80, secure: false);
		}

		public HttpServer(int port)
			: this(port, port == 443)
		{
		}

		public HttpServer(string url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			if (url.Length == 0)
			{
				throw new ArgumentException("An empty string.", "url");
			}
			if (!tryCreateUri(url, out Uri result, out string message))
			{
				throw new ArgumentException(message, "url");
			}
			string dnsSafeHost = result.GetDnsSafeHost(bracketIPv6: true);
			IPAddress iPAddress = dnsSafeHost.ToIPAddress();
			if (iPAddress == null)
			{
				message = "The host part could not be converted to an IP address.";
				throw new ArgumentException(message, "url");
			}
			if (!iPAddress.IsLocal())
			{
				message = "The IP address of the host is not a local IP address.";
				throw new ArgumentException(message, "url");
			}
			init(dnsSafeHost, iPAddress, result.Port, result.Scheme == "https");
		}

		public HttpServer(int port, bool secure)
		{
			if (!port.IsPortNumber())
			{
				string message = "Less than 1 or greater than 65535.";
				throw new ArgumentOutOfRangeException("port", message);
			}
			init("*", IPAddress.Any, port, secure);
		}

		public HttpServer(IPAddress address, int port)
			: this(address, port, port == 443)
		{
		}

		public HttpServer(IPAddress address, int port, bool secure)
		{
			if (address == null)
			{
				throw new ArgumentNullException("address");
			}
			if (!address.IsLocal())
			{
				throw new ArgumentException("Not a local IP address.", "address");
			}
			if (!port.IsPortNumber())
			{
				string message = "Less than 1 or greater than 65535.";
				throw new ArgumentOutOfRangeException("port", message);
			}
			init(address.ToString(bracketIPv6: true), address, port, secure);
		}

		private void abort()
		{
			lock (_sync)
			{
				if (_state != ServerState.Start)
				{
					return;
				}
				_state = ServerState.ShuttingDown;
			}
			try
			{
				try
				{
					_services.Stop(1006, string.Empty);
				}
				finally
				{
					_listener.Abort();
				}
			}
			catch
			{
			}
			_state = ServerState.Stop;
		}

		private bool canSet(out string message)
		{
			message = null;
			if (_state == ServerState.Start)
			{
				message = "The server has already started.";
				return false;
			}
			if (_state == ServerState.ShuttingDown)
			{
				message = "The server is shutting down.";
				return false;
			}
			return true;
		}

		private bool checkCertificate(out string message)
		{
			message = null;
			bool flag = _listener.SslConfiguration.ServerCertificate != null;
			string certificateFolderPath = _listener.CertificateFolderPath;
			bool flag2 = EndPointListener.CertificateExists(_port, certificateFolderPath);
			if (!(flag || flag2))
			{
				message = "There is no server certificate for secure connection.";
				return false;
			}
			if (flag && flag2)
			{
				_log.Warn("The server certificate associated with the port is used.");
			}
			return true;
		}

		private string createFilePath(string childPath)
		{
			childPath = childPath.TrimStart('/', '\\');
			return new StringBuilder(_docRootPath, 32).AppendFormat("/{0}", childPath).ToString().Replace('\\', '/');
		}

		private static WebSocketSharp.Net.HttpListener createListener(string hostname, int port, bool secure)
		{
			WebSocketSharp.Net.HttpListener httpListener = new WebSocketSharp.Net.HttpListener();
			string arg = secure ? "https" : "http";
			string uriPrefix = $"{arg}://{hostname}:{port}/";
			httpListener.Prefixes.Add(uriPrefix);
			return httpListener;
		}

		private void init(string hostname, IPAddress address, int port, bool secure)
		{
			_hostname = hostname;
			_address = address;
			_port = port;
			_secure = secure;
			_docRootPath = "./Public";
			_listener = createListener(_hostname, _port, _secure);
			_log = _listener.Log;
			_services = new WebSocketServiceManager(_log);
			_sync = new object();
		}

		private void processRequest(WebSocketSharp.Net.HttpListenerContext context)
		{
			object obj;
			switch (context.Request.HttpMethod)
			{
			default:
				obj = null;
				break;
			case "TRACE":
				obj = this.OnTrace;
				break;
			case "OPTIONS":
				obj = this.OnOptions;
				break;
			case "CONNECT":
				obj = this.OnConnect;
				break;
			case "DELETE":
				obj = this.OnDelete;
				break;
			case "PUT":
				obj = this.OnPut;
				break;
			case "POST":
				obj = this.OnPost;
				break;
			case "HEAD":
				obj = this.OnHead;
				break;
			case "GET":
				obj = this.OnGet;
				break;
			}
			EventHandler<HttpRequestEventArgs> eventHandler = (EventHandler<HttpRequestEventArgs>)obj;
			if (eventHandler != null)
			{
				eventHandler(this, new HttpRequestEventArgs(context, _docRootPath));
			}
			else
			{
				context.Response.StatusCode = 501;
			}
			context.Response.Close();
		}

		private void processRequest(HttpListenerWebSocketContext context)
		{
			Uri requestUri = context.RequestUri;
			if (requestUri == null)
			{
				context.Close(WebSocketSharp.Net.HttpStatusCode.BadRequest);
				return;
			}
			string absolutePath = requestUri.AbsolutePath;
			if (!_services.InternalTryGetServiceHost(absolutePath, out WebSocketServiceHost host))
			{
				context.Close(WebSocketSharp.Net.HttpStatusCode.NotImplemented);
			}
			else
			{
				host.StartSession(context);
			}
		}

		private void receiveRequest()
		{
			while (true)
			{
				WebSocketSharp.Net.HttpListenerContext ctx = null;
				try
				{
					ctx = _listener.GetContext();
					ThreadPool.QueueUserWorkItem(delegate
					{
						try
						{
							if (ctx.Request.IsUpgradeRequest("websocket"))
							{
								processRequest(ctx.AcceptWebSocket(null));
							}
							else
							{
								processRequest(ctx);
							}
						}
						catch (Exception ex4)
						{
							_log.Fatal(ex4.Message);
							_log.Debug(ex4.ToString());
							ctx.Connection.Close(force: true);
						}
					});
				}
				catch (WebSocketSharp.Net.HttpListenerException)
				{
					_log.Info("The underlying listener is stopped.");
					break;
				}
				catch (InvalidOperationException)
				{
					_log.Info("The underlying listener is stopped.");
					break;
				}
				catch (Exception ex3)
				{
					_log.Fatal(ex3.Message);
					_log.Debug(ex3.ToString());
					if (ctx != null)
					{
						ctx.Connection.Close(force: true);
					}
					break;
				}
			}
			if (_state != ServerState.ShuttingDown)
			{
				abort();
			}
		}

		private void start()
		{
			if (_state == ServerState.Start)
			{
				_log.Info("The server has already started.");
				return;
			}
			if (_state == ServerState.ShuttingDown)
			{
				_log.Warn("The server is shutting down.");
				return;
			}
			lock (_sync)
			{
				if (_state == ServerState.Start)
				{
					_log.Info("The server has already started.");
					return;
				}
				if (_state == ServerState.ShuttingDown)
				{
					_log.Warn("The server is shutting down.");
					return;
				}
				_services.Start();
				try
				{
					startReceiving();
				}
				catch
				{
					_services.Stop(1011, string.Empty);
					throw;
				}
				_state = ServerState.Start;
			}
		}

		private void startReceiving()
		{
			try
			{
				_listener.Start();
			}
			catch (Exception innerException)
			{
				string message = "The underlying listener has failed to start.";
				throw new InvalidOperationException(message, innerException);
			}
			_receiveThread = new Thread(receiveRequest);
			_receiveThread.IsBackground = true;
			_receiveThread.Start();
		}

		private void stop(ushort code, string reason)
		{
			if (_state == ServerState.Ready)
			{
				_log.Info("The server is not started.");
				return;
			}
			if (_state == ServerState.ShuttingDown)
			{
				_log.Info("The server is shutting down.");
				return;
			}
			if (_state == ServerState.Stop)
			{
				_log.Info("The server has already stopped.");
				return;
			}
			lock (_sync)
			{
				if (_state == ServerState.ShuttingDown)
				{
					_log.Info("The server is shutting down.");
					return;
				}
				if (_state == ServerState.Stop)
				{
					_log.Info("The server has already stopped.");
					return;
				}
				_state = ServerState.ShuttingDown;
			}
			try
			{
				bool flag = false;
				try
				{
					_services.Stop(code, reason);
				}
				catch
				{
					flag = true;
					throw;
				}
				finally
				{
					try
					{
						stopReceiving(5000);
					}
					catch
					{
						if (!flag)
						{
							throw;
						}
					}
				}
			}
			finally
			{
				_state = ServerState.Stop;
			}
		}

		private void stopReceiving(int millisecondsTimeout)
		{
			_listener.Stop();
			_receiveThread.Join(millisecondsTimeout);
		}

		private static bool tryCreateUri(string uriString, out Uri result, out string message)
		{
			result = null;
			message = null;
			Uri uri = uriString.ToUri();
			if (uri == null)
			{
				message = "An invalid URI string.";
				return false;
			}
			if (!uri.IsAbsoluteUri)
			{
				message = "A relative URI.";
				return false;
			}
			string scheme = uri.Scheme;
			if (!(scheme == "http") && !(scheme == "https"))
			{
				message = "The scheme part is not 'http' or 'https'.";
				return false;
			}
			if (uri.PathAndQuery != "/")
			{
				message = "It includes either or both path and query components.";
				return false;
			}
			if (uri.Fragment.Length > 0)
			{
				message = "It includes the fragment component.";
				return false;
			}
			if (uri.Port == 0)
			{
				message = "The port part is zero.";
				return false;
			}
			result = uri;
			return true;
		}

		[Obsolete("This method will be removed. Use added one instead.")]
		public void AddWebSocketService<TBehavior>(string path, Func<TBehavior> creator) where TBehavior : WebSocketBehavior
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (creator == null)
			{
				throw new ArgumentNullException("creator");
			}
			if (path.Length == 0)
			{
				throw new ArgumentException("An empty string.", "path");
			}
			if (path[0] != '/')
			{
				throw new ArgumentException("Not an absolute path.", "path");
			}
			if (path.IndexOfAny(new char[2]
			{
				'?',
				'#'
			}) > -1)
			{
				string message = "It includes either or both query and fragment components.";
				throw new ArgumentException(message, "path");
			}
			_services.Add(path, creator);
		}

		public void AddWebSocketService<TBehaviorWithNew>(string path) where TBehaviorWithNew : WebSocketBehavior, new()
		{
			_services.AddService<TBehaviorWithNew>(path, null);
		}

		public void AddWebSocketService<TBehaviorWithNew>(string path, Action<TBehaviorWithNew> initializer) where TBehaviorWithNew : WebSocketBehavior, new()
		{
			_services.AddService(path, initializer);
		}

		[Obsolete("This method will be removed.")]
		public byte[] GetFile(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (path.Length == 0)
			{
				throw new ArgumentException("An empty string.", "path");
			}
			if (path.IndexOf("..") > -1)
			{
				throw new ArgumentException("It contains '..'.", "path");
			}
			path = createFilePath(path);
			return File.Exists(path) ? File.ReadAllBytes(path) : null;
		}

		public bool RemoveWebSocketService(string path)
		{
			return _services.RemoveService(path);
		}

		public void Start()
		{
			if (_secure && !checkCertificate(out string message))
			{
				throw new InvalidOperationException(message);
			}
			start();
		}

		public void Stop()
		{
			stop(1001, string.Empty);
		}

		[Obsolete("This method will be removed.")]
		public void Stop(ushort code, string reason)
		{
			if (!code.IsCloseStatusCode())
			{
				string message = "Less than 1000 or greater than 4999.";
				throw new ArgumentOutOfRangeException("code", message);
			}
			if (code == 1010)
			{
				string message2 = "1010 cannot be used.";
				throw new ArgumentException(message2, "code");
			}
			if (!reason.IsNullOrEmpty())
			{
				if (code == 1005)
				{
					string message3 = "1005 cannot be used.";
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
			}
			stop(code, reason);
		}

		[Obsolete("This method will be removed.")]
		public void Stop(CloseStatusCode code, string reason)
		{
			if (code == CloseStatusCode.MandatoryExtension)
			{
				string message = "MandatoryExtension cannot be used.";
				throw new ArgumentException(message, "code");
			}
			if (!reason.IsNullOrEmpty())
			{
				if (code == CloseStatusCode.NoStatus)
				{
					string message2 = "NoStatus cannot be used.";
					throw new ArgumentException(message2, "code");
				}
				if (!reason.TryGetUTF8EncodedBytes(out byte[] bytes))
				{
					string message3 = "It could not be UTF-8-encoded.";
					throw new ArgumentException(message3, "reason");
				}
				if (bytes.Length > 123)
				{
					string message4 = "Its size is greater than 123 bytes.";
					throw new ArgumentOutOfRangeException("reason", message4);
				}
			}
			stop((ushort)code, reason);
		}
	}
}
