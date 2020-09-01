using System;

namespace Tom.Exceptions
{
	public class SFSCodecError : Exception
	{
		public SFSCodecError(string message)
			: base(message)
		{
		}
	}
}
