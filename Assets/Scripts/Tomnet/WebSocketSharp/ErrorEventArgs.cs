using System;

namespace WebSocketSharp
{
	public class ErrorEventArgs : EventArgs
	{
		private Exception _exception;

		private string _message;

		public Exception Exception => _exception;

		public string Message => _message;

		internal ErrorEventArgs(string message)
			: this(message, null)
		{
		}

		internal ErrorEventArgs(string message, Exception exception)
		{
			_message = message;
			_exception = exception;
		}
	}
}
