using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace WebSocketSharp.Net
{
	[Serializable]
	[ComVisible(true)]
	public class WebHeaderCollection : NameValueCollection, ISerializable
	{
		private static readonly Dictionary<string, HttpHeaderInfo> _headers;

		private bool _internallyUsed;

		private HttpHeaderType _state;

		internal HttpHeaderType State => _state;

		public override string[] AllKeys => base.AllKeys;

		public override int Count => base.Count;

		public string this[HttpRequestHeader header]
		{
			get
			{
				return Get(Convert(header));
			}
			set
			{
				Add(header, value);
			}
		}

		public string this[HttpResponseHeader header]
		{
			get
			{
				return Get(Convert(header));
			}
			set
			{
				Add(header, value);
			}
		}

		public override KeysCollection Keys => base.Keys;

		static WebHeaderCollection()
		{
			_headers = new Dictionary<string, HttpHeaderInfo>(StringComparer.InvariantCultureIgnoreCase)
			{
				{
					"Accept",
					new HttpHeaderInfo("Accept", HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"AcceptCharset",
					new HttpHeaderInfo("Accept-Charset", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"AcceptEncoding",
					new HttpHeaderInfo("Accept-Encoding", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"AcceptLanguage",
					new HttpHeaderInfo("Accept-Language", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"AcceptRanges",
					new HttpHeaderInfo("Accept-Ranges", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Age",
					new HttpHeaderInfo("Age", HttpHeaderType.Response)
				},
				{
					"Allow",
					new HttpHeaderInfo("Allow", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Authorization",
					new HttpHeaderInfo("Authorization", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"CacheControl",
					new HttpHeaderInfo("Cache-Control", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Connection",
					new HttpHeaderInfo("Connection", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"ContentEncoding",
					new HttpHeaderInfo("Content-Encoding", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"ContentLanguage",
					new HttpHeaderInfo("Content-Language", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"ContentLength",
					new HttpHeaderInfo("Content-Length", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"ContentLocation",
					new HttpHeaderInfo("Content-Location", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ContentMd5",
					new HttpHeaderInfo("Content-MD5", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ContentRange",
					new HttpHeaderInfo("Content-Range", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ContentType",
					new HttpHeaderInfo("Content-Type", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"Cookie",
					new HttpHeaderInfo("Cookie", HttpHeaderType.Request)
				},
				{
					"Cookie2",
					new HttpHeaderInfo("Cookie2", HttpHeaderType.Request)
				},
				{
					"Date",
					new HttpHeaderInfo("Date", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"Expect",
					new HttpHeaderInfo("Expect", HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"Expires",
					new HttpHeaderInfo("Expires", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ETag",
					new HttpHeaderInfo("ETag", HttpHeaderType.Response)
				},
				{
					"From",
					new HttpHeaderInfo("From", HttpHeaderType.Request)
				},
				{
					"Host",
					new HttpHeaderInfo("Host", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"IfMatch",
					new HttpHeaderInfo("If-Match", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"IfModifiedSince",
					new HttpHeaderInfo("If-Modified-Since", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"IfNoneMatch",
					new HttpHeaderInfo("If-None-Match", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"IfRange",
					new HttpHeaderInfo("If-Range", HttpHeaderType.Request)
				},
				{
					"IfUnmodifiedSince",
					new HttpHeaderInfo("If-Unmodified-Since", HttpHeaderType.Request)
				},
				{
					"KeepAlive",
					new HttpHeaderInfo("Keep-Alive", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"LastModified",
					new HttpHeaderInfo("Last-Modified", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"Location",
					new HttpHeaderInfo("Location", HttpHeaderType.Response)
				},
				{
					"MaxForwards",
					new HttpHeaderInfo("Max-Forwards", HttpHeaderType.Request)
				},
				{
					"Pragma",
					new HttpHeaderInfo("Pragma", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ProxyAuthenticate",
					new HttpHeaderInfo("Proxy-Authenticate", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"ProxyAuthorization",
					new HttpHeaderInfo("Proxy-Authorization", HttpHeaderType.Request)
				},
				{
					"ProxyConnection",
					new HttpHeaderInfo("Proxy-Connection", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"Public",
					new HttpHeaderInfo("Public", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Range",
					new HttpHeaderInfo("Range", HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"Referer",
					new HttpHeaderInfo("Referer", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"RetryAfter",
					new HttpHeaderInfo("Retry-After", HttpHeaderType.Response)
				},
				{
					"SecWebSocketAccept",
					new HttpHeaderInfo("Sec-WebSocket-Accept", HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"SecWebSocketExtensions",
					new HttpHeaderInfo("Sec-WebSocket-Extensions", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValueInRequest)
				},
				{
					"SecWebSocketKey",
					new HttpHeaderInfo("Sec-WebSocket-Key", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"SecWebSocketProtocol",
					new HttpHeaderInfo("Sec-WebSocket-Protocol", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValueInRequest)
				},
				{
					"SecWebSocketVersion",
					new HttpHeaderInfo("Sec-WebSocket-Version", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValueInResponse)
				},
				{
					"Server",
					new HttpHeaderInfo("Server", HttpHeaderType.Response)
				},
				{
					"SetCookie",
					new HttpHeaderInfo("Set-Cookie", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"SetCookie2",
					new HttpHeaderInfo("Set-Cookie2", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Te",
					new HttpHeaderInfo("TE", HttpHeaderType.Request)
				},
				{
					"Trailer",
					new HttpHeaderInfo("Trailer", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"TransferEncoding",
					new HttpHeaderInfo("Transfer-Encoding", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"Translate",
					new HttpHeaderInfo("Translate", HttpHeaderType.Request)
				},
				{
					"Upgrade",
					new HttpHeaderInfo("Upgrade", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"UserAgent",
					new HttpHeaderInfo("User-Agent", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"Vary",
					new HttpHeaderInfo("Vary", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Via",
					new HttpHeaderInfo("Via", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Warning",
					new HttpHeaderInfo("Warning", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"WwwAuthenticate",
					new HttpHeaderInfo("WWW-Authenticate", HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				}
			};
		}

		internal WebHeaderCollection(HttpHeaderType state, bool internallyUsed)
		{
			_state = state;
			_internallyUsed = internallyUsed;
		}

		protected WebHeaderCollection(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			if (serializationInfo == null)
			{
				throw new ArgumentNullException("serializationInfo");
			}
			try
			{
				_internallyUsed = serializationInfo.GetBoolean("InternallyUsed");
				_state = (HttpHeaderType)serializationInfo.GetInt32("State");
				int @int = serializationInfo.GetInt32("Count");
				for (int i = 0; i < @int; i++)
				{
					base.Add(serializationInfo.GetString(i.ToString()), serializationInfo.GetString((@int + i).ToString()));
				}
			}
			catch (SerializationException ex)
			{
				throw new ArgumentException(ex.Message, "serializationInfo", ex);
			}
		}

		public WebHeaderCollection()
		{
		}

		private void add(string name, string value, bool ignoreRestricted)
		{
			Action<string, string> action = ignoreRestricted ? new Action<string, string>(addWithoutCheckingNameAndRestricted) : new Action<string, string>(addWithoutCheckingName);
			doWithCheckingState(action, checkName(name), value, setState: true);
		}

		private void addWithoutCheckingName(string name, string value)
		{
			doWithoutCheckingName(base.Add, name, value);
		}

		private void addWithoutCheckingNameAndRestricted(string name, string value)
		{
			base.Add(name, checkValue(value));
		}

		private static int checkColonSeparated(string header)
		{
			int num = header.IndexOf(':');
			if (num == -1)
			{
				throw new ArgumentException("No colon could be found.", "header");
			}
			return num;
		}

		private static HttpHeaderType checkHeaderType(string name)
		{
			HttpHeaderInfo headerInfo = getHeaderInfo(name);
			return (headerInfo != null) ? ((headerInfo.IsRequest && !headerInfo.IsResponse) ? HttpHeaderType.Request : ((!headerInfo.IsRequest && headerInfo.IsResponse) ? HttpHeaderType.Response : HttpHeaderType.Unspecified)) : HttpHeaderType.Unspecified;
		}

		private static string checkName(string name)
		{
			if (name == null || name.Length == 0)
			{
				throw new ArgumentNullException("name");
			}
			name = name.Trim();
			if (!IsHeaderName(name))
			{
				throw new ArgumentException("Contains invalid characters.", "name");
			}
			return name;
		}

		private void checkRestricted(string name)
		{
			if (!_internallyUsed && isRestricted(name, response: true))
			{
				throw new ArgumentException("This header must be modified with the appropiate property.");
			}
		}

		private void checkState(bool response)
		{
			if (_state != 0)
			{
				if (response && _state == HttpHeaderType.Request)
				{
					throw new InvalidOperationException("This collection has already been used to store the request headers.");
				}
				if (!response && _state == HttpHeaderType.Response)
				{
					throw new InvalidOperationException("This collection has already been used to store the response headers.");
				}
			}
		}

		private static string checkValue(string value)
		{
			if (value == null || value.Length == 0)
			{
				return string.Empty;
			}
			value = value.Trim();
			if (value.Length > 65535)
			{
				throw new ArgumentOutOfRangeException("value", "Greater than 65,535 characters.");
			}
			if (!IsHeaderValue(value))
			{
				throw new ArgumentException("Contains invalid characters.", "value");
			}
			return value;
		}

		private static string convert(string key)
		{
			HttpHeaderInfo value;
			return _headers.TryGetValue(key, out value) ? value.Name : string.Empty;
		}

		private void doWithCheckingState(Action<string, string> action, string name, string value, bool setState)
		{
			switch (checkHeaderType(name))
			{
			case HttpHeaderType.Request:
				doWithCheckingState(action, name, value, response: false, setState);
				break;
			case HttpHeaderType.Response:
				doWithCheckingState(action, name, value, response: true, setState);
				break;
			default:
				action(name, value);
				break;
			}
		}

		private void doWithCheckingState(Action<string, string> action, string name, string value, bool response, bool setState)
		{
			checkState(response);
			action(name, value);
			if (setState && _state == HttpHeaderType.Unspecified)
			{
				_state = ((!response) ? HttpHeaderType.Request : HttpHeaderType.Response);
			}
		}

		private void doWithoutCheckingName(Action<string, string> action, string name, string value)
		{
			checkRestricted(name);
			action(name, checkValue(value));
		}

		private static HttpHeaderInfo getHeaderInfo(string name)
		{
			foreach (HttpHeaderInfo value in _headers.Values)
			{
				if (value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
				{
					return value;
				}
			}
			return null;
		}

		private static bool isRestricted(string name, bool response)
		{
			return getHeaderInfo(name)?.IsRestricted(response) ?? false;
		}

		private void removeWithoutCheckingName(string name, string unuse)
		{
			checkRestricted(name);
			base.Remove(name);
		}

		private void setWithoutCheckingName(string name, string value)
		{
			doWithoutCheckingName(base.Set, name, value);
		}

		internal static string Convert(HttpRequestHeader header)
		{
			return convert(header.ToString());
		}

		internal static string Convert(HttpResponseHeader header)
		{
			return convert(header.ToString());
		}

		internal void InternalRemove(string name)
		{
			base.Remove(name);
		}

		internal void InternalSet(string header, bool response)
		{
			int num = checkColonSeparated(header);
			InternalSet(header.Substring(0, num), header.Substring(num + 1), response);
		}

		internal void InternalSet(string name, string value, bool response)
		{
			value = checkValue(value);
			if (IsMultiValue(name, response))
			{
				base.Add(name, value);
			}
			else
			{
				base.Set(name, value);
			}
		}

		internal static bool IsHeaderName(string name)
		{
			return name != null && name.Length > 0 && name.IsToken();
		}

		internal static bool IsHeaderValue(string value)
		{
			return value.IsText();
		}

		internal static bool IsMultiValue(string headerName, bool response)
		{
			if (headerName == null || headerName.Length == 0)
			{
				return false;
			}
			return getHeaderInfo(headerName)?.IsMultiValue(response) ?? false;
		}

		internal string ToStringMultiValue(bool response)
		{
			StringBuilder buff = new StringBuilder();
			Count.Times(delegate(int i)
			{
				string key = GetKey(i);
				if (IsMultiValue(key, response))
				{
					string[] values = GetValues(i);
					foreach (string arg in values)
					{
						buff.AppendFormat("{0}: {1}\r\n", key, arg);
					}
				}
				else
				{
					buff.AppendFormat("{0}: {1}\r\n", key, Get(i));
				}
			});
			return buff.Append("\r\n").ToString();
		}

		protected void AddWithoutValidate(string headerName, string headerValue)
		{
			add(headerName, headerValue, ignoreRestricted: true);
		}

		public void Add(string header)
		{
			if (header == null || header.Length == 0)
			{
				throw new ArgumentNullException("header");
			}
			int num = checkColonSeparated(header);
			add(header.Substring(0, num), header.Substring(num + 1), ignoreRestricted: false);
		}

		public void Add(HttpRequestHeader header, string value)
		{
			doWithCheckingState(addWithoutCheckingName, Convert(header), value, response: false, setState: true);
		}

		public void Add(HttpResponseHeader header, string value)
		{
			doWithCheckingState(addWithoutCheckingName, Convert(header), value, response: true, setState: true);
		}

		public override void Add(string name, string value)
		{
			add(name, value, ignoreRestricted: false);
		}

		public override void Clear()
		{
			base.Clear();
			_state = HttpHeaderType.Unspecified;
		}

		public override string Get(int index)
		{
			return base.Get(index);
		}

		public override string Get(string name)
		{
			return base.Get(name);
		}

		public override IEnumerator GetEnumerator()
		{
			return base.GetEnumerator();
		}

		public override string GetKey(int index)
		{
			return base.GetKey(index);
		}

		public override string[] GetValues(int index)
		{
			string[] values = base.GetValues(index);
			return (values != null && values.Length != 0) ? values : null;
		}

		public override string[] GetValues(string header)
		{
			string[] values = base.GetValues(header);
			return (values != null && values.Length != 0) ? values : null;
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			if (serializationInfo == null)
			{
				throw new ArgumentNullException("serializationInfo");
			}
			serializationInfo.AddValue("InternallyUsed", _internallyUsed);
			serializationInfo.AddValue("State", (int)_state);
			int cnt = Count;
			serializationInfo.AddValue("Count", cnt);
			cnt.Times(delegate(int i)
			{
				serializationInfo.AddValue(i.ToString(), GetKey(i));
				serializationInfo.AddValue((cnt + i).ToString(), Get(i));
			});
		}

		public static bool IsRestricted(string headerName)
		{
			return isRestricted(checkName(headerName), response: false);
		}

		public static bool IsRestricted(string headerName, bool response)
		{
			return isRestricted(checkName(headerName), response);
		}

		public override void OnDeserialization(object sender)
		{
		}

		public void Remove(HttpRequestHeader header)
		{
			doWithCheckingState(removeWithoutCheckingName, Convert(header), null, response: false, setState: false);
		}

		public void Remove(HttpResponseHeader header)
		{
			doWithCheckingState(removeWithoutCheckingName, Convert(header), null, response: true, setState: false);
		}

		public override void Remove(string name)
		{
			doWithCheckingState(removeWithoutCheckingName, checkName(name), null, setState: false);
		}

		public void Set(HttpRequestHeader header, string value)
		{
			doWithCheckingState(setWithoutCheckingName, Convert(header), value, response: false, setState: true);
		}

		public void Set(HttpResponseHeader header, string value)
		{
			doWithCheckingState(setWithoutCheckingName, Convert(header), value, response: true, setState: true);
		}

		public override void Set(string name, string value)
		{
			doWithCheckingState(setWithoutCheckingName, checkName(name), value, setState: true);
		}

		public byte[] ToByteArray()
		{
			return Encoding.UTF8.GetBytes(ToString());
		}

		public override string ToString()
		{
			StringBuilder buff = new StringBuilder();
			Count.Times(delegate(int i)
			{
				buff.AppendFormat("{0}: {1}\r\n", GetKey(i), Get(i));
			});
			return buff.Append("\r\n").ToString();
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter, SerializationFormatter = true)]
		void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			GetObjectData(serializationInfo, streamingContext);
		}
	}
}
