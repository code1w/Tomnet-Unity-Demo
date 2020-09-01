using System;
using System.Collections.Specialized;
using System.Text;

namespace WebSocketSharp.Net
{
	internal abstract class AuthenticationBase
	{
		private AuthenticationSchemes _scheme;

		internal NameValueCollection Parameters;

		public string Algorithm => Parameters["algorithm"];

		public string Nonce => Parameters["nonce"];

		public string Opaque => Parameters["opaque"];

		public string Qop => Parameters["qop"];

		public string Realm => Parameters["realm"];

		public AuthenticationSchemes Scheme => _scheme;

		protected AuthenticationBase(AuthenticationSchemes scheme, NameValueCollection parameters)
		{
			_scheme = scheme;
			Parameters = parameters;
		}

		internal static string CreateNonceValue()
		{
			byte[] array = new byte[16];
			Random random = new Random();
			random.NextBytes(array);
			StringBuilder stringBuilder = new StringBuilder(32);
			byte[] array2 = array;
			foreach (byte b in array2)
			{
				stringBuilder.Append(b.ToString("x2"));
			}
			return stringBuilder.ToString();
		}

		internal static NameValueCollection ParseParameters(string value)
		{
			NameValueCollection nameValueCollection = new NameValueCollection();
			foreach (string item in value.SplitHeaderValue(','))
			{
				int num = item.IndexOf('=');
				string name = (num > 0) ? item.Substring(0, num).Trim() : null;
				string value2 = (num < 0) ? item.Trim().Trim('"') : ((num < item.Length - 1) ? item.Substring(num + 1).Trim().Trim('"') : string.Empty);
				nameValueCollection.Add(name, value2);
			}
			return nameValueCollection;
		}

		internal abstract string ToBasicString();

		internal abstract string ToDigestString();

		public override string ToString()
		{
			return (_scheme == AuthenticationSchemes.Basic) ? ToBasicString() : ((_scheme == AuthenticationSchemes.Digest) ? ToDigestString() : string.Empty);
		}
	}
}
