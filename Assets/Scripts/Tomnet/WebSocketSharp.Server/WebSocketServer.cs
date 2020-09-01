using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp.Server
{
	public class WebSocketServer
	{
		private IPAddress _address;

		private bool _allowForwardedRequest;

		private WebSocketSharp.Net.AuthenticationSchemes _authSchemes;

		private static readonly string _defaultRealm;

		private bool _dnsStyle;

		private string _hostname;

		private TcpListener _listener;

		private Logger _log;

		private int _port;

		private string _realm;

		private string _realmInUse;

		private Thread _receiveThread;

		private bool _reuseAddress;

		private bool _secure;

		private WebSocketServiceManager _services;

		private ServerSslConfiguration _sslConfig;

		private ServerSslConfiguration _sslConfigInUse;

		private volatile ServerState _state;

		private object _sync;

		private Func<IIdentity, WebSocketSharp.Net.NetworkCredential> _userCredFinder;

		public IPAddress Address => _address;

		public bool AllowForwardedRequest
		{
			get
			{
				return _allowForwardedRequest;
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
						_allowForwardedRequest = value;
					}
				}
			}
		}

		public WebSocketSharp.Net.AuthenticationSchemes AuthenticationSchemes
		{
			get
			{
				return _authSchemes;
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
						_authSchemes = value;
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
				return _realm;
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
						_realm = value;
					}
				}
			}
		}

		public bool ReuseAddress
		{
			get
			{
				return _reuseAddress;
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
						_reuseAddress = value;
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
				return getSslConfiguration();
			}
		}

		public Func<IIdentity, WebSocketSharp.Net.NetworkCredential> UserCredentialsFinder
		{
			get
			{
				return _userCredFinder;
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
						_userCredFinder = value;
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

		static WebSocketServer()
		{
			_defaultRealm = "SECRET AREA";
		}

		public WebSocketServer()
		{
			IPAddress any = IPAddress.Any;
			init(any.ToString(), any, 80, secure: false);
		}

		public WebSocketServer(int port)
			: this(port, port == 443)
		{
		}

		public WebSocketServer(string url)
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
			string dnsSafeHost = result.DnsSafeHost;
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
			init(dnsSafeHost, iPAddress, result.Port, result.Scheme == "wss");
		}

		public WebSocketServer(int port, bool secure)
		{
			if (!port.IsPortNumber())
			{
				string message = "Less than 1 or greater than 65535.";
				throw new ArgumentOutOfRangeException("port", message);
			}
			IPAddress any = IPAddress.Any;
			init(any.ToString(), any, port, secure);
		}

		public WebSocketServer(IPAddress address, int port)
			: this(address, port, port == 443)
		{
		}

		public WebSocketServer(IPAddress address, int port, bool secure)
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
			init(address.ToString(), address, port, secure);
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
					_listener.Stop();
				}
				finally
				{
					_services.Stop(1006, string.Empty);
				}
			}
			catch
			{
			}
			_state = ServerState.Stop;
		}

		private bool authenticateClient(TcpListenerWebSocketContext context)
		{
			if (_authSchemes == WebSocketSharp.Net.AuthenticationSchemes.Anonymous)
			{
				return true;
			}
			if (_authSchemes == WebSocketSharp.Net.AuthenticationSchemes.None)
			{
				return false;
			}
			return context.Authenticate(_authSchemes, _realmInUse, _userCredFinder);
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

		private bool checkHostNameForRequest(string name)
		{
			return !_dnsStyle || Uri.CheckHostName(name) != UriHostNameType.Dns || name == _hostname;
		}

		private static bool checkSslConfiguration(ServerSslConfiguration configuration, out string message)
		{
			message = null;
			if (configuration.ServerCertificate == null)
			{
				message = "There is no server certificate for secure connection.";
				return false;
			}
			return true;
		}

		private string getRealm()
		{
			string realm = _realm;
			return (realm != null && realm.Length > 0) ? realm : _defaultRealm;
		}

		private ServerSslConfiguration getSslConfiguration()
		{
			if (_sslConfig == null)
			{
				_sslConfig = new ServerSslConfiguration();
			}
			return _sslConfig;
		}

		private void init(string hostname, IPAddress address, int port, bool secure)
		{
			_hostname = hostname;
			_address = address;
			_port = port;
			_secure = secure;
			_authSchemes = WebSocketSharp.Net.AuthenticationSchemes.Anonymous;
			_dnsStyle = (Uri.CheckHostName(hostname) == UriHostNameType.Dns);
			_listener = new TcpListener(address, port);
			_log = new Logger();
			_services = new WebSocketServiceManager(_log);
			_sync = new object();
		}

		private void processRequest(TcpListenerWebSocketContext context)
		{
			if (!authenticateClient(context))
			{
				context.Close(WebSocketSharp.Net.HttpStatusCode.Forbidden);
				return;
			}
			Uri requestUri = context.RequestUri;
			if (requestUri == null)
			{
				context.Close(WebSocketSharp.Net.HttpStatusCode.BadRequest);
				return;
			}
			if (!_allowForwardedRequest)
			{
				if (requestUri.Port != _port)
				{
					context.Close(WebSocketSharp.Net.HttpStatusCode.BadRequest);
					return;
				}
				if (!checkHostNameForRequest(requestUri.DnsSafeHost))
				{
					context.Close(WebSocketSharp.Net.HttpStatusCode.NotFound);
					return;
				}
			}
			if (!_services.InternalTryGetServiceHost(requestUri.AbsolutePath, out WebSocketServiceHost host))
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
				TcpClient cl = null;
				try
				{
					cl = _listener.AcceptTcpClient();
					ThreadPool.QueueUserWorkItem(delegate
					{
						try
						{
							TcpListenerWebSocketContext context = new TcpListenerWebSocketContext(cl, null, _secure, _sslConfigInUse, _log);
							processRequest(context);
						}
						catch (Exception ex3)
						{
							_log.Error(ex3.Message);
							_log.Debug(ex3.ToString());
							cl.Close();
						}
					});
				}
				catch (SocketException ex)
				{
					if (_state == ServerState.ShuttingDown)
					{
						_log.Info("The underlying listener is stopped.");
						break;
					}
					_log.Fatal(ex.Message);
					_log.Debug(ex.ToString());
					break;
				}
				catch (Exception ex2)
				{
					_log.Fatal(ex2.Message);
					_log.Debug(ex2.ToString());
					if (cl != null)
					{
						cl.Close();
					}
					break;
				}
			}
			if (_state != ServerState.ShuttingDown)
			{
				abort();
			}
		}

		private void start(ServerSslConfiguration sslConfig)
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
				_sslConfigInUse = sslConfig;
				_realmInUse = getRealm();
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
			if (_reuseAddress)
			{
				_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			}
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
					stopReceiving(5000);
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
						_services.Stop(code, reason);
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
			try
			{
				_listener.Stop();
			}
			catch (Exception innerException)
			{
				string message = "The underlying listener has failed to stop.";
				throw new InvalidOperationException(message, innerException);
			}
			_receiveThread.Join(millisecondsTimeout);
		}

		private static bool tryCreateUri(string uriString, out Uri result, out string message)
		{
			if (!uriString.TryCreateWebSocketUri(out result, out message))
			{
				return false;
			}
			if (result.PathAndQuery != "/")
			{
				result = null;
				message = "It includes either or both path and query components.";
				return false;
			}
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

		public bool RemoveWebSocketService(string path)
		{
			return _services.RemoveService(path);
		}

		public void Start()
		{
			ServerSslConfiguration serverSslConfiguration = null;
			if (_secure)
			{
				serverSslConfiguration = new ServerSslConfiguration(getSslConfiguration());
				if (!checkSslConfiguration(serverSslConfiguration, out string message))
				{
					throw new InvalidOperationException(message);
				}
			}
			start(serverSslConfiguration);
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
