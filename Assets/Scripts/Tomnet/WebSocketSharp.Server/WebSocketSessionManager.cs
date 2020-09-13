using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

namespace WebSocketSharp.Server
{
    public class WebSocketSessionManager
    {
        private volatile bool _clean;

        private object _forSweep;

        private Logger _log;

        private Dictionary<string, IWebSocketSession> _sessions;

        private volatile ServerState _state;

        private volatile bool _sweeping;

        private System.Timers.Timer _sweepTimer;

        private object _sync;

        private TimeSpan _waitTime;

        internal ServerState State => _state;

        public IEnumerable<string> ActiveIDs
        {
            get
            {
                foreach (KeyValuePair<string, bool> res in broadping(WebSocketFrame.EmptyPingBytes))
                {
                    if (res.Value)
                    {
                        yield return res.Key;
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_sync)
                {
                    return _sessions.Count;
                }
            }
        }

        public IEnumerable<string> IDs
        {
            get
            {
                if (_state != ServerState.Start)
                {
                    return Enumerable.Empty<string>();
                }
                lock (_sync)
                {
                    if (_state != ServerState.Start)
                    {
                        return Enumerable.Empty<string>();
                    }
                    return _sessions.Keys.ToList();
                }
            }
        }

        public IEnumerable<string> InactiveIDs
        {
            get
            {
                foreach (KeyValuePair<string, bool> res in broadping(WebSocketFrame.EmptyPingBytes))
                {
                    if (!res.Value)
                    {
                        yield return res.Key;
                    }
                }
            }
        }

        public IWebSocketSession this[string id]
        {
            get
            {
                if (id == null)
                {
                    throw new ArgumentNullException("id");
                }
                if (id.Length == 0)
                {
                    throw new ArgumentException("An empty string.", "id");
                }
                tryGetSession(id, out IWebSocketSession session);
                return session;
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
                    }
                    else
                    {
                        _clean = value;
                    }
                }
            }
        }

        public IEnumerable<IWebSocketSession> Sessions
        {
            get
            {
                if (_state != ServerState.Start)
                {
                    return Enumerable.Empty<IWebSocketSession>();
                }
                lock (_sync)
                {
                    if (_state != ServerState.Start)
                    {
                        return Enumerable.Empty<IWebSocketSession>();
                    }
                    return _sessions.Values.ToList();
                }
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
                    }
                    else
                    {
                        _waitTime = value;
                    }
                }
            }
        }

        internal WebSocketSessionManager(Logger log)
        {
            _log = log;
            _clean = true;
            _forSweep = new object();
            _sessions = new Dictionary<string, IWebSocketSession>();
            _state = ServerState.Ready;
            _sync = ((ICollection)_sessions).SyncRoot;
            _waitTime = TimeSpan.FromSeconds(1.0);
            setSweepTimer(60000.0);
        }

        private void broadcast(Opcode opcode, byte[] data, Action completed)
        {
            Dictionary<CompressionMethod, byte[]> dictionary = new Dictionary<CompressionMethod, byte[]>();
            try
            {
                foreach (IWebSocketSession session in Sessions)
                {
                    if (_state != ServerState.Start)
                    {
                        _log.Error("The service is shutting down.");
                        break;
                    }
                    session.Context.WebSocket.Send(opcode, data, dictionary);
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
                foreach (IWebSocketSession session in Sessions)
                {
                    if (_state != ServerState.Start)
                    {
                        _log.Error("The service is shutting down.");
                        break;
                    }
                    session.Context.WebSocket.Send(opcode, stream, dictionary);
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

        private Dictionary<string, bool> broadping(byte[] frameAsBytes)
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            foreach (IWebSocketSession session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                bool value = session.Context.WebSocket.Ping(frameAsBytes, _waitTime);
                dictionary.Add(session.ID, value);
            }
            return dictionary;
        }

        private bool canSet(out string message)
        {
            message = null;
            if (_state == ServerState.Start)
            {
                message = "The service has already started.";
                return false;
            }
            if (_state == ServerState.ShuttingDown)
            {
                message = "The service is shutting down.";
                return false;
            }
            return true;
        }

        private static string createID()
        {
            return Guid.NewGuid().ToString("N");
        }

        private void setSweepTimer(double interval)
        {
            _sweepTimer = new System.Timers.Timer(interval);
            _sweepTimer.Elapsed += delegate
            {
                Sweep();
            };
        }

        private void stop(PayloadData payloadData, bool send)
        {
            byte[] frameAsBytes = send ? WebSocketFrame.CreateCloseFrame(payloadData, mask: false).ToArray() : null;
            lock (_sync)
            {
                _state = ServerState.ShuttingDown;
                _sweepTimer.Enabled = false;
                foreach (IWebSocketSession item in _sessions.Values.ToList())
                {
                    item.Context.WebSocket.Close(payloadData, frameAsBytes);
                }
                _state = ServerState.Stop;
            }
        }

        private bool tryGetSession(string id, out IWebSocketSession session)
        {
            session = null;
            if (_state != ServerState.Start)
            {
                return false;
            }
            lock (_sync)
            {
                if (_state != ServerState.Start)
                {
                    return false;
                }
                return _sessions.TryGetValue(id, out session);
            }
        }

        internal string Add(IWebSocketSession session)
        {
            lock (_sync)
            {
                if (_state != ServerState.Start)
                {
                    return null;
                }
                string text = createID();
                _sessions.Add(text, session);
                return text;
            }
        }

        internal void Broadcast(Opcode opcode, byte[] data, Dictionary<CompressionMethod, byte[]> cache)
        {
            foreach (IWebSocketSession session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                session.Context.WebSocket.Send(opcode, data, cache);
            }
        }

        internal void Broadcast(Opcode opcode, Stream stream, Dictionary<CompressionMethod, Stream> cache)
        {
            foreach (IWebSocketSession session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                session.Context.WebSocket.Send(opcode, stream, cache);
            }
        }

        internal Dictionary<string, bool> Broadping(byte[] frameAsBytes, TimeSpan timeout)
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            foreach (IWebSocketSession session in Sessions)
            {
                if (_state != ServerState.Start)
                {
                    _log.Error("The service is shutting down.");
                    break;
                }
                bool value = session.Context.WebSocket.Ping(frameAsBytes, timeout);
                dictionary.Add(session.ID, value);
            }
            return dictionary;
        }

        internal bool Remove(string id)
        {
            lock (_sync)
            {
                return _sessions.Remove(id);
            }
        }

        internal void Start()
        {
            lock (_sync)
            {
                _sweepTimer.Enabled = _clean;
                _state = ServerState.Start;
            }
        }

        internal void Stop(ushort code, string reason)
        {
            if (code == 1005)
            {
                stop(PayloadData.Empty, send: true);
            }
            else
            {
                stop(new PayloadData(code, reason), !code.IsReserved());
            }
        }

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

        public void Broadcast(Stream stream, int length)
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
                broadcast(Opcode.Binary, array, null);
            }
            else
            {
                broadcast(Opcode.Binary, new MemoryStream(array), null);
            }
        }

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
        public Dictionary<string, bool> Broadping()
        {
            if (_state != ServerState.Start)
            {
                string message = "The current state of the manager is not Start.";
                throw new InvalidOperationException(message);
            }
            return Broadping(WebSocketFrame.EmptyPingBytes, _waitTime);
        }

        [Obsolete("This method will be removed.")]
        public Dictionary<string, bool> Broadping(string message)
        {
            if (_state != ServerState.Start)
            {
                string message2 = "The current state of the manager is not Start.";
                throw new InvalidOperationException(message2);
            }
            if (message.IsNullOrEmpty())
            {
                return Broadping(WebSocketFrame.EmptyPingBytes, _waitTime);
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
            return Broadping(webSocketFrame.ToArray(), _waitTime);
        }

        public void CloseSession(string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.Close();
        }

        public void CloseSession(string id, ushort code, string reason)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.Close(code, reason);
        }

        public void CloseSession(string id, CloseStatusCode code, string reason)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.Close(code, reason);
        }

        public bool PingTo(string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            return session.Context.WebSocket.Ping();
        }

        public bool PingTo(string message, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message2 = "The session could not be found.";
                throw new InvalidOperationException(message2);
            }
            return session.Context.WebSocket.Ping(message);
        }

        public void SendTo(byte[] data, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.Send(data);
        }

        public void SendTo(string data, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.Send(data);
        }

        public void SendTo(Stream stream, int length, string id)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.Send(stream, length);
        }

        public void SendToAsync(byte[] data, string id, Action<bool> completed)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.SendAsync(data, completed);
        }

        public void SendToAsync(string data, string id, Action<bool> completed)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.SendAsync(data, completed);
        }

        public void SendToAsync(Stream stream, int length, string id, Action<bool> completed)
        {
            if (!TryGetSession(id, out IWebSocketSession session))
            {
                string message = "The session could not be found.";
                throw new InvalidOperationException(message);
            }
            session.Context.WebSocket.SendAsync(stream, length, completed);
        }

        public void Sweep()
        {
            if (_sweeping)
            {
                _log.Info("The sweeping is already in progress.");
                return;
            }
            lock (_forSweep)
            {
                if (_sweeping)
                {
                    _log.Info("The sweeping is already in progress.");
                    return;
                }
                _sweeping = true;
            }
            foreach (string inactiveID in InactiveIDs)
            {
                if (_state != ServerState.Start)
                {
                    break;
                }
                lock (_sync)
                {
                    if (_state != ServerState.Start)
                    {
                        break;
                    }
                    if (_sessions.TryGetValue(inactiveID, out IWebSocketSession value))
                    {
                        switch (value.ConnectionState)
                        {
                            case WebSocketState.Open:
                                value.Context.WebSocket.Close(CloseStatusCode.Abnormal);
                                break;
                            default:
                                _sessions.Remove(inactiveID);
                                break;
                            case WebSocketState.Closing:
                                break;
                        }
                    }
                    continue;
                }
            }
            _sweeping = false;
        }

        public bool TryGetSession(string id, out IWebSocketSession session)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (id.Length == 0)
            {
                throw new ArgumentException("An empty string.", "id");
            }
            return tryGetSession(id, out session);
        }
    }
}
