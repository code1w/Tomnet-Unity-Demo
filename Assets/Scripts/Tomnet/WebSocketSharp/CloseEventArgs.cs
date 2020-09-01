using System;

namespace WebSocketSharp
{
	public class CloseEventArgs : EventArgs
	{
		private bool _clean;

		private PayloadData _payloadData;

		internal PayloadData PayloadData => _payloadData;

		public ushort Code => _payloadData.Code;

		public string Reason => _payloadData.Reason ?? string.Empty;

		public bool WasClean
		{
			get
			{
				return _clean;
			}
			internal set
			{
				_clean = value;
			}
		}

		internal CloseEventArgs()
		{
			_payloadData = PayloadData.Empty;
		}

		internal CloseEventArgs(ushort code)
			: this(code, null)
		{
		}

		internal CloseEventArgs(CloseStatusCode code)
			: this((ushort)code, null)
		{
		}

		internal CloseEventArgs(PayloadData payloadData)
		{
			_payloadData = payloadData;
		}

		internal CloseEventArgs(ushort code, string reason)
		{
			_payloadData = new PayloadData(code, reason);
		}

		internal CloseEventArgs(CloseStatusCode code, string reason)
			: this((ushort)code, reason)
		{
		}
	}
}
