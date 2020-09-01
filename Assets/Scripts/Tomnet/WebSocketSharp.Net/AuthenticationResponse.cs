using System;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace WebSocketSharp.Net
{
	internal class AuthenticationResponse : AuthenticationBase
	{
		private uint _nonceCount;

		internal uint NonceCount => (_nonceCount < uint.MaxValue) ? _nonceCount : 0u;

		public string Cnonce => Parameters["cnonce"];

		public string Nc => Parameters["nc"];

		public string Password => Parameters["password"];

		public string Response => Parameters["response"];

		public string Uri => Parameters["uri"];

		public string UserName => Parameters["username"];

		private AuthenticationResponse(AuthenticationSchemes scheme, NameValueCollection parameters)
			: base(scheme, parameters)
		{
		}

		internal AuthenticationResponse(NetworkCredential credentials)
			: this(AuthenticationSchemes.Basic, new NameValueCollection(), credentials, 0u)
		{
		}

		internal AuthenticationResponse(AuthenticationChallenge challenge, NetworkCredential credentials, uint nonceCount)
			: this(challenge.Scheme, challenge.Parameters, credentials, nonceCount)
		{
		}

		internal AuthenticationResponse(AuthenticationSchemes scheme, NameValueCollection parameters, NetworkCredential credentials, uint nonceCount)
			: base(scheme, parameters)
		{
			Parameters["username"] = credentials.Username;
			Parameters["password"] = credentials.Password;
			Parameters["uri"] = credentials.Domain;
			_nonceCount = nonceCount;
			if (scheme == AuthenticationSchemes.Digest)
			{
				initAsDigest();
			}
		}

		private static string createA1(string username, string password, string realm)
		{
			return $"{username}:{realm}:{password}";
		}

		private static string createA1(string username, string password, string realm, string nonce, string cnonce)
		{
			return $"{hash(createA1(username, password, realm))}:{nonce}:{cnonce}";
		}

		private static string createA2(string method, string uri)
		{
			return $"{method}:{uri}";
		}

		private static string createA2(string method, string uri, string entity)
		{
			return $"{method}:{uri}:{hash(entity)}";
		}

		private static string hash(string value)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			MD5 mD = MD5.Create();
			byte[] array = mD.ComputeHash(bytes);
			StringBuilder stringBuilder = new StringBuilder(64);
			byte[] array2 = array;
			foreach (byte b in array2)
			{
				stringBuilder.Append(b.ToString("x2"));
			}
			return stringBuilder.ToString();
		}

		private void initAsDigest()
		{
			string text = Parameters["qop"];
			if (text != null)
			{
				if (text.Split(',').Contains((string qop) => qop.Trim().ToLower() == "auth"))
				{
					Parameters["qop"] = "auth";
					Parameters["cnonce"] = AuthenticationBase.CreateNonceValue();
					Parameters["nc"] = $"{++_nonceCount:x8}";
				}
				else
				{
					Parameters["qop"] = null;
				}
			}
			Parameters["method"] = "GET";
			Parameters["response"] = CreateRequestDigest(Parameters);
		}

		internal static string CreateRequestDigest(NameValueCollection parameters)
		{
			string username = parameters["username"];
			string password = parameters["password"];
			string realm = parameters["realm"];
			string text = parameters["nonce"];
			string uri = parameters["uri"];
			string text2 = parameters["algorithm"];
			string text3 = parameters["qop"];
			string text4 = parameters["cnonce"];
			string text5 = parameters["nc"];
			string method = parameters["method"];
			string value = (text2 != null && text2.ToLower() == "md5-sess") ? createA1(username, password, realm, text, text4) : createA1(username, password, realm);
			string value2 = (text3 != null && text3.ToLower() == "auth-int") ? createA2(method, uri, parameters["entity"]) : createA2(method, uri);
			string arg = hash(value);
			string arg2 = (text3 != null) ? $"{text}:{text5}:{text4}:{text3}:{hash(value2)}" : $"{text}:{hash(value2)}";
			return hash($"{arg}:{arg2}");
		}

		internal static AuthenticationResponse Parse(string value)
		{
			try
			{
				string[] array = value.Split(new char[1]
				{
					' '
				}, 2);
				if (array.Length != 2)
				{
					return null;
				}
				string a = array[0].ToLower();
				return (a == "basic") ? new AuthenticationResponse(AuthenticationSchemes.Basic, ParseBasicCredentials(array[1])) : ((a == "digest") ? new AuthenticationResponse(AuthenticationSchemes.Digest, AuthenticationBase.ParseParameters(array[1])) : null);
			}
			catch
			{
			}
			return null;
		}

		internal static NameValueCollection ParseBasicCredentials(string value)
		{
			string @string = Encoding.Default.GetString(Convert.FromBase64String(value));
			int num = @string.IndexOf(':');
			string text = @string.Substring(0, num);
			string value2 = (num < @string.Length - 1) ? @string.Substring(num + 1) : string.Empty;
			num = text.IndexOf('\\');
			if (num > -1)
			{
				text = text.Substring(num + 1);
			}
			NameValueCollection nameValueCollection = new NameValueCollection();
			nameValueCollection["username"] = text;
			nameValueCollection["password"] = value2;
			return nameValueCollection;
		}

		internal override string ToBasicString()
		{
			string s = string.Format("{0}:{1}", Parameters["username"], Parameters["password"]);
			string str = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
			return "Basic " + str;
		}

		internal override string ToDigestString()
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			stringBuilder.AppendFormat("Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", response=\"{4}\"", Parameters["username"], Parameters["realm"], Parameters["nonce"], Parameters["uri"], Parameters["response"]);
			string text = Parameters["opaque"];
			if (text != null)
			{
				stringBuilder.AppendFormat(", opaque=\"{0}\"", text);
			}
			string text2 = Parameters["algorithm"];
			if (text2 != null)
			{
				stringBuilder.AppendFormat(", algorithm={0}", text2);
			}
			string text3 = Parameters["qop"];
			if (text3 != null)
			{
				stringBuilder.AppendFormat(", qop={0}, cnonce=\"{1}\", nc={2}", text3, Parameters["cnonce"], Parameters["nc"]);
			}
			return stringBuilder.ToString();
		}

		public IIdentity ToIdentity()
		{
			AuthenticationSchemes scheme = base.Scheme;
			IIdentity result;
			if (scheme != AuthenticationSchemes.Basic)
			{
				IIdentity identity = (scheme == AuthenticationSchemes.Digest) ? new HttpDigestIdentity(Parameters) : null;
				result = identity;
			}
			else
			{
				IIdentity identity = new HttpBasicIdentity(Parameters["username"], Parameters["password"]);
				result = identity;
			}
			return result;
		}
	}
}
