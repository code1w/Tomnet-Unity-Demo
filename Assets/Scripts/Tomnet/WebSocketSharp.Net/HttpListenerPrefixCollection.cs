using System;
using System.Collections;
using System.Collections.Generic;

namespace WebSocketSharp.Net
{
	public class HttpListenerPrefixCollection : ICollection<string>, IEnumerable<string>, IEnumerable
	{
		private HttpListener _listener;

		private List<string> _prefixes;

		public int Count => _prefixes.Count;

		public bool IsReadOnly => false;

		public bool IsSynchronized => false;

		internal HttpListenerPrefixCollection(HttpListener listener)
		{
			_listener = listener;
			_prefixes = new List<string>();
		}

		public void Add(string uriPrefix)
		{
			_listener.CheckDisposed();
			HttpListenerPrefix.CheckPrefix(uriPrefix);
			if (!_prefixes.Contains(uriPrefix))
			{
				_prefixes.Add(uriPrefix);
				if (_listener.IsListening)
				{
					EndPointManager.AddPrefix(uriPrefix, _listener);
				}
			}
		}

		public void Clear()
		{
			_listener.CheckDisposed();
			_prefixes.Clear();
			if (_listener.IsListening)
			{
				EndPointManager.RemoveListener(_listener);
			}
		}

		public bool Contains(string uriPrefix)
		{
			_listener.CheckDisposed();
			if (uriPrefix == null)
			{
				throw new ArgumentNullException("uriPrefix");
			}
			return _prefixes.Contains(uriPrefix);
		}

		public void CopyTo(Array array, int offset)
		{
			_listener.CheckDisposed();
			((ICollection)_prefixes).CopyTo(array, offset);
		}

		public void CopyTo(string[] array, int offset)
		{
			_listener.CheckDisposed();
			_prefixes.CopyTo(array, offset);
		}

		public IEnumerator<string> GetEnumerator()
		{
			return _prefixes.GetEnumerator();
		}

		public bool Remove(string uriPrefix)
		{
			_listener.CheckDisposed();
			if (uriPrefix == null)
			{
				throw new ArgumentNullException("uriPrefix");
			}
			bool flag = _prefixes.Remove(uriPrefix);
			if (flag && _listener.IsListening)
			{
				EndPointManager.RemovePrefix(uriPrefix, _listener);
			}
			return flag;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _prefixes.GetEnumerator();
		}
	}
}
