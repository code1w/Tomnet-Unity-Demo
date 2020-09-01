using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using WebSocketSharp.Net;

namespace WebSocketSharp.Server
{
	public class WebSocketServiceManager
	{
		private volatile bool _clean;

		private Dictionary<string, WebSocketServiceHost> _hosts;

		private Logger _log;

		private volatile ServerState _state;

		private object _sync;

		private TimeSpan _waitTime;

		public int Count
		{
			get
			{
				lock (_sync)
				{
					return _hosts.Count;
				}
			}
		}

		public IEnumerable<WebSocketServiceHost> Hosts
		{
			get
			{
				lock (_sync)
				{
					return _hosts.Values.ToList();
				}
			}
		}

		public WebSocketServiceHost this[string path]
		{
			get
			{
				if (path == null)
				{
					throw new ArgumentNullException("path");
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
				InternalTryGetServiceHost(path, out WebSocketServiceHost host);
				return host;
			}
		}

		public bool KeepClean
		{
			get
			{
				return _clean;
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
						return;
					}
					foreach (WebSocketServiceHost value2 in _hosts.Values)
					{
						value2.KeepClean = value;
					}
					_clean = value;
				}
			}
		}

		public IEnumerable<string> Paths
		{
			get
			{
				lock (_sync)
				{
					return _hosts.Keys.ToList();
				}
			}
		}

		[Obsolete("This property will be removed.")]
		public int SessionCount
		{
			get
			{
				int num = 0;
				foreach (WebSocketServiceHost host in Hosts)
				{
					if (_state != ServerState.Start)
					{
						break;
					}
					num += host.Sessions.Count;
				}
				return num;
			}
		}

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
					_log.Warn(message);
					return;
				}
				lock (_sync)
				{
					if (!canSet(out message))
					{
						_log.Warn(message);
						return;
					}
					foreach (WebSocketServiceHost value2 in _hosts.Values)
					{
						value2.WaitTime = value;
					}
					_waitTime = value;
				}
			}
		}

		internal WebSocketServiceManager(Logger log)
		{
			_log = log;
			_clean = true;
			_hosts = new Dictionary<string, WebSocketServiceHost>();
			_state = ServerState.Ready;
			_sync = ((ICollection)_hosts).SyncRoot;
			_waitTime = TimeSpan.FromSeconds(1.0);
		}

		private void broadcast(Opcode opcode, byte[] data, Action completed)
		{
			Dictionary<CompressionMethod, byte[]> dictionary = new Dictionary<CompressionMethod, byte[]>();
			try
			{
				foreach (WebSocketServiceHost host in Hosts)
				{
					if (_state != ServerState.Start)
					{
						_log.Error("The server is shutting down.");
						break;
					}
					host.Sessions.Broadcast(opcode, data, dictionary);
				}
				completed?.Invoke();
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
			}
			finally
			{
				dictionary.Clear();
			}
		}

		private void broadcast(Opcode opcode, Stream stream, Action completed)
		{
			Dictionary<CompressionMethod, Stream> dictionary = new Dictionary<CompressionMethod, Stream>();
			try
			{
				foreach (WebSocketServiceHost host in Hosts)
				{
					if (_state != ServerState.Start)
					{
						_log.Error("The server is shutting down.");
						break;
					}
					host.Sessions.Broadcast(opcode, stream, dictionary);
				}
				completed?.Invoke();
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
			}
			finally
			{
				foreach (Stream value in dictionary.Values)
				{
					value.Dispose();
				}
				dictionary.Clear();
			}
		}

		private void broadcastAsync(Opcode opcode, byte[] data, Action completed)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				broadcast(opcode, data, completed);
			});
		}

		private void broadcastAsync(Opcode opcode, Stream stream, Action completed)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				broadcast(opcode, stream, completed);
			});
		}

		private Dictionary<string, Dictionary<string, bool>> broadping(byte[] frameAsBytes, TimeSpan timeout)
		{
			Dictionary<string, Dictionary<string, bool>> dictionary = new Dictionary<string, Dictionary<string, bool>>();
			foreach (WebSocketServiceHost host in Hosts)
			{
				if (_state != ServerState.Start)
				{
					_log.Error("The server is shutting down.");
					break;
				}
				Dictionary<string, bool> value = host.Sessions.Broadping(frameAsBytes, timeout);
				dictionary.Add(host.Path, value);
			}
			return dictionary;
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

		internal void Add<TBehavior>(string path, Func<TBehavior> creator) where TBehavior : WebSocketBehavior
		{
			path = HttpUtility.UrlDecode(path).TrimSlashFromEnd();
			lock (_sync)
			{
				if (_hosts.TryGetValue(path, out WebSocketServiceHost value))
				{
					throw new ArgumentException("Already in use.", "path");
				}
				value = new WebSocketServiceHost<TBehavior>(path, creator, null, _log);
				if (!_clean)
				{
					value.KeepClean = false;
				}
				if (_waitTime != value.WaitTime)
				{
					value.WaitTime = _waitTime;
				}
				if (_state == ServerState.Start)
				{
					value.Start();
				}
				_hosts.Add(path, value);
			}
		}

		internal bool InternalTryGetServiceHost(string path, out WebSocketServiceHost host)
		{
			path = HttpUtility.UrlDecode(path).TrimSlashFromEnd();
			lock (_sync)
			{
				return _hosts.TryGetValue(path, out host);
			}
		}

		internal void Start()
		{
			lock (_sync)
			{
				foreach (WebSocketServiceHost value in _hosts.Values)
				{
					value.Start();
				}
				_state = ServerState.Start;
			}
		}

		internal void Stop(ushort code, string reason)
		{
			lock (_sync)
			{
				_state = ServerState.ShuttingDown;
				foreach (WebSocketServiceHost value in _hosts.Values)
				{
					value.Stop(code, reason);
				}
				_state = ServerState.Stop;
			}
		}

		public void AddService<TBehavior>(string path, Action<TBehavior> initializer) where TBehavior : WebSocketBehavior, new()
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
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
			path = HttpUtility.UrlDecode(path).TrimSlashFromEnd();
			lock (_sync)
			{
				if (_hosts.TryGetValue(path, out WebSocketServiceHost value))
				{
					throw new ArgumentException("Already in use.", "path");
				}
				value = new WebSocketServiceHost<TBehavior>(path, () => new TBehavior(), initializer, _log);
				if (!_clean)
				{
					value.KeepClean = false;
				}
				if (_waitTime != value.WaitTime)
				{
					value.WaitTime = _waitTime;
				}
				if (_state == ServerState.Start)
				{
					value.Start();
				}
				_hosts.Add(path, value);
			}
		}

		[Obsolete("This method will be removed.")]
		public void Broadcast(byte[] data)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the manager is not Start.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.LongLength <= WebSocket.FragmentLength)
			{
				broadcast(Opcode.Binary, data, null);
			}
			else
			{
				broadcast(Opcode.Binary, new MemoryStream(data), null);
			}
		}

		[Obsolete("This method will be removed.")]
		public void Broadcast(string data)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the manager is not Start.";
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
			if (bytes.LongLength <= WebSocket.FragmentLength)
			{
				broadcast(Opcode.Text, bytes, null);
			}
			else
			{
				broadcast(Opcode.Text, new MemoryStream(bytes), null);
			}
		}

		[Obsolete("This method will be removed.")]
		public void BroadcastAsync(byte[] data, Action completed)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the manager is not Start.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.LongLength <= WebSocket.FragmentLength)
			{
				broadcastAsync(Opcode.Binary, data, completed);
			}
			else
			{
				broadcastAsync(Opcode.Binary, new MemoryStream(data), completed);
			}
		}

		[Obsolete("This method will be removed.")]
		public void BroadcastAsync(string data, Action completed)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the manager is not Start.";
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
			if (bytes.LongLength <= WebSocket.FragmentLength)
			{
				broadcastAsync(Opcode.Text, bytes, completed);
			}
			else
			{
				broadcastAsync(Opcode.Text, new MemoryStream(bytes), completed);
			}
		}

		[Obsolete("This method will be removed.")]
		public void BroadcastAsync(Stream stream, int length, Action completed)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the manager is not Start.";
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
				_log.Warn($"Only {num} byte(s) of data could be read from the stream.");
			}
			if (num <= WebSocket.FragmentLength)
			{
				broadcastAsync(Opcode.Binary, array, completed);
			}
			else
			{
				broadcastAsync(Opcode.Binary, new MemoryStream(array), completed);
			}
		}

		[Obsolete("This method will be removed.")]
		public Dictionary<string, Dictionary<string, bool>> Broadping()
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the manager is not Start.";
				throw new InvalidOperationException(message);
			}
			return broadping(WebSocketFrame.EmptyPingBytes, _waitTime);
		}

		[Obsolete("This method will be removed.")]
		public Dictionary<string, Dictionary<string, bool>> Broadping(string message)
		{
			if (_state != ServerState.Start)
			{
				string message2 = "The current state of the manager is not Start.";
				throw new InvalidOperationException(message2);
			}
			if (message.IsNullOrEmpty())
			{
				return broadping(WebSocketFrame.EmptyPingBytes, _waitTime);
			}
			if (!message.TryGetUTF8EncodedBytes(out byte[] bytes))
			{
				string message3 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message3, "message");
			}
			if (bytes.Length > 125)
			{
				string message4 = "Its size is greater than 125 bytes.";
				throw new ArgumentOutOfRangeException("message", message4);
			}
			WebSocketFrame webSocketFrame = WebSocketFrame.CreatePingFrame(bytes, mask: false);
			return broadping(webSocketFrame.ToArray(), _waitTime);
		}

		public void Clear()
		{
			List<WebSocketServiceHost> list = null;
			lock (_sync)
			{
				list = _hosts.Values.ToList();
				_hosts.Clear();
			}
			foreach (WebSocketServiceHost item in list)
			{
				if (item.State == ServerState.Start)
				{
					item.Stop(1001, string.Empty);
				}
			}
		}

		public bool RemoveService(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
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
			path = HttpUtility.UrlDecode(path).TrimSlashFromEnd();
			WebSocketServiceHost value;
			lock (_sync)
			{
				if (!_hosts.TryGetValue(path, out value))
				{
					return false;
				}
				_hosts.Remove(path);
			}
			if (value.State == ServerState.Start)
			{
				value.Stop(1001, string.Empty);
			}
			return true;
		}

		public bool TryGetServiceHost(string path, out WebSocketServiceHost host)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
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
			return InternalTryGetServiceHost(path, out host);
		}
	}
}
