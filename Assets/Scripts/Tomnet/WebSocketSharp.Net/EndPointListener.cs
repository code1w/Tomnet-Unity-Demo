using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace WebSocketSharp.Net
{
	internal sealed class EndPointListener
	{
		private List<HttpListenerPrefix> _all;

		private static readonly string _defaultCertFolderPath;

		private IPEndPoint _endpoint;

		private Dictionary<HttpListenerPrefix, HttpListener> _prefixes;

		private bool _secure;

		private Socket _socket;

		private ServerSslConfiguration _sslConfig;

		private List<HttpListenerPrefix> _unhandled;

		private Dictionary<HttpConnection, HttpConnection> _unregistered;

		private object _unregisteredSync;

		public IPAddress Address => _endpoint.Address;

		public bool IsSecure => _secure;

		public int Port => _endpoint.Port;

		public ServerSslConfiguration SslConfiguration => _sslConfig;

		static EndPointListener()
		{
			_defaultCertFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}

		internal EndPointListener(IPEndPoint endpoint, bool secure, string certificateFolderPath, ServerSslConfiguration sslConfig, bool reuseAddress)
		{
			if (secure)
			{
				X509Certificate2 certificate = getCertificate(endpoint.Port, certificateFolderPath, sslConfig.ServerCertificate);
				if (certificate == null)
				{
					throw new ArgumentException("No server certificate could be found.");
				}
				_secure = true;
				_sslConfig = new ServerSslConfiguration(sslConfig);
				_sslConfig.ServerCertificate = certificate;
			}
			_endpoint = endpoint;
			_prefixes = new Dictionary<HttpListenerPrefix, HttpListener>();
			_unregistered = new Dictionary<HttpConnection, HttpConnection>();
			_unregisteredSync = ((ICollection)_unregistered).SyncRoot;
			_socket = new Socket(endpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			if (reuseAddress)
			{
				_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			}
			_socket.Bind(endpoint);
			_socket.Listen(500);
			_socket.BeginAccept(onAccept, this);
		}

		private static void addSpecial(List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
		{
			string path = prefix.Path;
			foreach (HttpListenerPrefix prefix2 in prefixes)
			{
				if (prefix2.Path == path)
				{
					throw new HttpListenerException(87, "The prefix is already in use.");
				}
			}
			prefixes.Add(prefix);
		}

		private static RSACryptoServiceProvider createRSAFromFile(string filename)
		{
			byte[] array = null;
			using (FileStream fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				array = new byte[fileStream.Length];
				fileStream.Read(array, 0, array.Length);
			}
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.ImportCspBlob(array);
			return rSACryptoServiceProvider;
		}

		private static X509Certificate2 getCertificate(int port, string folderPath, X509Certificate2 defaultCertificate)
		{
			if (folderPath == null || folderPath.Length == 0)
			{
				folderPath = _defaultCertFolderPath;
			}
			try
			{
				string text = Path.Combine(folderPath, $"{port}.cer");
				string text2 = Path.Combine(folderPath, $"{port}.key");
				if (File.Exists(text) && File.Exists(text2))
				{
					X509Certificate2 x509Certificate = new X509Certificate2(text);
					x509Certificate.PrivateKey = createRSAFromFile(text2);
					return x509Certificate;
				}
			}
			catch
			{
			}
			return defaultCertificate;
		}

		private void leaveIfNoPrefix()
		{
			if (_prefixes.Count > 0)
			{
				return;
			}
			List<HttpListenerPrefix> unhandled = _unhandled;
			if (unhandled == null || unhandled.Count <= 0)
			{
				unhandled = _all;
				if (unhandled == null || unhandled.Count <= 0)
				{
					EndPointManager.RemoveEndPoint(_endpoint);
				}
			}
		}

		private static void onAccept(IAsyncResult asyncResult)
		{
			EndPointListener endPointListener = (EndPointListener)asyncResult.AsyncState;
			Socket socket = null;
			try
			{
				socket = endPointListener._socket.EndAccept(asyncResult);
			}
			catch (SocketException)
			{
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			try
			{
				endPointListener._socket.BeginAccept(onAccept, endPointListener);
			}
			catch
			{
				socket?.Close();
				return;
			}
			if (socket != null)
			{
				processAccepted(socket, endPointListener);
			}
		}

		private static void processAccepted(Socket socket, EndPointListener listener)
		{
			HttpConnection httpConnection = null;
			try
			{
				httpConnection = new HttpConnection(socket, listener);
				lock (listener._unregisteredSync)
				{
					listener._unregistered[httpConnection] = httpConnection;
				}
				httpConnection.BeginReadRequest();
			}
			catch
			{
				if (httpConnection != null)
				{
					httpConnection.Close(force: true);
				}
				else
				{
					socket.Close();
				}
			}
		}

		private static bool removeSpecial(List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
		{
			string path = prefix.Path;
			int count = prefixes.Count;
			for (int i = 0; i < count; i++)
			{
				if (prefixes[i].Path == path)
				{
					prefixes.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		private static HttpListener searchHttpListenerFromSpecial(string path, List<HttpListenerPrefix> prefixes)
		{
			if (prefixes == null)
			{
				return null;
			}
			HttpListener result = null;
			int num = -1;
			foreach (HttpListenerPrefix prefix in prefixes)
			{
				string path2 = prefix.Path;
				int length = path2.Length;
				if (length >= num && path.StartsWith(path2))
				{
					num = length;
					result = prefix.Listener;
				}
			}
			return result;
		}

		internal static bool CertificateExists(int port, string folderPath)
		{
			if (folderPath == null || folderPath.Length == 0)
			{
				folderPath = _defaultCertFolderPath;
			}
			string path = Path.Combine(folderPath, $"{port}.cer");
			string path2 = Path.Combine(folderPath, $"{port}.key");
			return File.Exists(path) && File.Exists(path2);
		}

		internal void RemoveConnection(HttpConnection connection)
		{
			lock (_unregisteredSync)
			{
				_unregistered.Remove(connection);
			}
		}

		internal bool TrySearchHttpListener(Uri uri, out HttpListener listener)
		{
			listener = null;
			if (uri == null)
			{
				return false;
			}
			string host = uri.Host;
			bool flag = Uri.CheckHostName(host) == UriHostNameType.Dns;
			string b = uri.Port.ToString();
			string text = HttpUtility.UrlDecode(uri.AbsolutePath);
			string text2 = (text[text.Length - 1] != '/') ? (text + "/") : text;
			if (host != null && host.Length > 0)
			{
				int num = -1;
				foreach (HttpListenerPrefix key in _prefixes.Keys)
				{
					if (flag)
					{
						string host2 = key.Host;
						if (Uri.CheckHostName(host2) == UriHostNameType.Dns && host2 != host)
						{
							continue;
						}
					}
					if (!(key.Port != b))
					{
						string path = key.Path;
						int length = path.Length;
						if (length >= num && (text.StartsWith(path) || text2.StartsWith(path)))
						{
							num = length;
							listener = _prefixes[key];
						}
					}
				}
				if (num != -1)
				{
					return true;
				}
			}
			List<HttpListenerPrefix> unhandled = _unhandled;
			listener = searchHttpListenerFromSpecial(text, unhandled);
			if (listener == null && text2 != text)
			{
				listener = searchHttpListenerFromSpecial(text2, unhandled);
			}
			if (listener != null)
			{
				return true;
			}
			unhandled = _all;
			listener = searchHttpListenerFromSpecial(text, unhandled);
			if (listener == null && text2 != text)
			{
				listener = searchHttpListenerFromSpecial(text2, unhandled);
			}
			return listener != null;
		}

		public void AddPrefix(HttpListenerPrefix prefix, HttpListener listener)
		{
			if (prefix.Host == "*")
			{
				List<HttpListenerPrefix> unhandled;
				List<HttpListenerPrefix> list;
				do
				{
					unhandled = _unhandled;
					list = ((unhandled != null) ? new List<HttpListenerPrefix>(unhandled) : new List<HttpListenerPrefix>());
					prefix.Listener = listener;
					addSpecial(list, prefix);
				}
				while (Interlocked.CompareExchange(ref _unhandled, list, unhandled) != unhandled);
				return;
			}
			if (prefix.Host == "+")
			{
				List<HttpListenerPrefix> unhandled;
				List<HttpListenerPrefix> list;
				do
				{
					unhandled = _all;
					list = ((unhandled != null) ? new List<HttpListenerPrefix>(unhandled) : new List<HttpListenerPrefix>());
					prefix.Listener = listener;
					addSpecial(list, prefix);
				}
				while (Interlocked.CompareExchange(ref _all, list, unhandled) != unhandled);
				return;
			}
			Dictionary<HttpListenerPrefix, HttpListener> prefixes;
			Dictionary<HttpListenerPrefix, HttpListener> dictionary;
			do
			{
				prefixes = _prefixes;
				if (prefixes.ContainsKey(prefix))
				{
					if (prefixes[prefix] != listener)
					{
						throw new HttpListenerException(87, $"There's another listener for {prefix}.");
					}
					break;
				}
				dictionary = new Dictionary<HttpListenerPrefix, HttpListener>(prefixes);
				dictionary[prefix] = listener;
			}
			while (Interlocked.CompareExchange(ref _prefixes, dictionary, prefixes) != prefixes);
		}

		public void Close()
		{
			_socket.Close();
			HttpConnection[] array = null;
			lock (_unregisteredSync)
			{
				if (_unregistered.Count == 0)
				{
					return;
				}
				Dictionary<HttpConnection, HttpConnection>.KeyCollection keys = _unregistered.Keys;
				array = new HttpConnection[keys.Count];
				keys.CopyTo(array, 0);
				_unregistered.Clear();
			}
			for (int num = array.Length - 1; num >= 0; num--)
			{
				array[num].Close(force: true);
			}
		}

		public void RemovePrefix(HttpListenerPrefix prefix, HttpListener listener)
		{
			if (prefix.Host == "*")
			{
				List<HttpListenerPrefix> unhandled;
				List<HttpListenerPrefix> list;
				do
				{
					unhandled = _unhandled;
					if (unhandled == null)
					{
						break;
					}
					list = new List<HttpListenerPrefix>(unhandled);
				}
				while (removeSpecial(list, prefix) && Interlocked.CompareExchange(ref _unhandled, list, unhandled) != unhandled);
				leaveIfNoPrefix();
				return;
			}
			if (prefix.Host == "+")
			{
				List<HttpListenerPrefix> unhandled;
				List<HttpListenerPrefix> list;
				do
				{
					unhandled = _all;
					if (unhandled == null)
					{
						break;
					}
					list = new List<HttpListenerPrefix>(unhandled);
				}
				while (removeSpecial(list, prefix) && Interlocked.CompareExchange(ref _all, list, unhandled) != unhandled);
				leaveIfNoPrefix();
				return;
			}
			Dictionary<HttpListenerPrefix, HttpListener> prefixes;
			Dictionary<HttpListenerPrefix, HttpListener> dictionary;
			do
			{
				prefixes = _prefixes;
				if (!prefixes.ContainsKey(prefix))
				{
					break;
				}
				dictionary = new Dictionary<HttpListenerPrefix, HttpListener>(prefixes);
				dictionary.Remove(prefix);
			}
			while (Interlocked.CompareExchange(ref _prefixes, dictionary, prefixes) != prefixes);
			leaveIfNoPrefix();
		}
	}
}
