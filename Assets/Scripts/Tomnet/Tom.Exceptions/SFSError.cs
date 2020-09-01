using System;

namespace Tom.Exceptions
{
	public class SFSError : Exception
	{
		public SFSError(string message)
			: base(message)
		{
		}
	}
}
