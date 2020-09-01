namespace WebSocketSharp.Net
{
	internal class HttpHeaderInfo
	{
		private string _name;

		private HttpHeaderType _type;

		internal bool IsMultiValueInRequest => (_type & HttpHeaderType.MultiValueInRequest) == HttpHeaderType.MultiValueInRequest;

		internal bool IsMultiValueInResponse => (_type & HttpHeaderType.MultiValueInResponse) == HttpHeaderType.MultiValueInResponse;

		public bool IsRequest => (_type & HttpHeaderType.Request) == HttpHeaderType.Request;

		public bool IsResponse => (_type & HttpHeaderType.Response) == HttpHeaderType.Response;

		public string Name => _name;

		public HttpHeaderType Type => _type;

		internal HttpHeaderInfo(string name, HttpHeaderType type)
		{
			_name = name;
			_type = type;
		}

		public bool IsMultiValue(bool response)
		{
			return ((_type & HttpHeaderType.MultiValue) != HttpHeaderType.MultiValue) ? (response ? IsMultiValueInResponse : IsMultiValueInRequest) : (response ? IsResponse : IsRequest);
		}

		public bool IsRestricted(bool response)
		{
			return (_type & HttpHeaderType.Restricted) == HttpHeaderType.Restricted && (response ? IsResponse : IsRequest);
		}
	}
}
