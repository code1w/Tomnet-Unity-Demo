using System.Collections.Specialized;
using System.Security.Principal;

namespace WebSocketSharp.Net
{
	public class HttpDigestIdentity : GenericIdentity
	{
		private NameValueCollection _parameters;

		public string Algorithm => _parameters["algorithm"];

		public string Cnonce => _parameters["cnonce"];

		public string Nc => _parameters["nc"];

		public string Nonce => _parameters["nonce"];

		public string Opaque => _parameters["opaque"];

		public string Qop => _parameters["qop"];

		public string Realm => _parameters["realm"];

		public string Response => _parameters["response"];

		public string Uri => _parameters["uri"];

		internal HttpDigestIdentity(NameValueCollection parameters)
			: base(parameters["username"], "Digest")
		{
			_parameters = parameters;
		}

		internal bool IsValid(string password, string realm, string method, string entity)
		{
			NameValueCollection nameValueCollection = new NameValueCollection(_parameters);
			nameValueCollection["password"] = password;
			nameValueCollection["realm"] = realm;
			nameValueCollection["method"] = method;
			nameValueCollection["entity"] = entity;
			string b = AuthenticationResponse.CreateRequestDigest(nameValueCollection);
			return _parameters["response"] == b;
		}
	}
}
