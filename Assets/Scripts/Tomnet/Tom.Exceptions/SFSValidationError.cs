using System;
using System.Collections.Generic;

namespace Tom.Exceptions
{
	public class SFSValidationError : Exception
	{
		private List<string> errors;

		public List<string> Errors => errors;

		public SFSValidationError(string message, ICollection<string> errors)
			: base(message)
		{
			this.errors = new List<string>(errors);
		}
	}
}
