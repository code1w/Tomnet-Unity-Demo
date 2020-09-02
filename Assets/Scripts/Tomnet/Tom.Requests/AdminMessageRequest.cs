using Tom.Entities.Data;
using System;

namespace Tom.Requests
{
	public class AdminMessageRequest : GenericMessageRequest
	{
		public AdminMessageRequest(string message, MessageRecipientMode recipientMode, ISFSObject parameters)
		{
			if (recipientMode == null)
			{
				throw new ArgumentException("RecipientMode cannot be null!");
			}
			type = 3;
			base.message = message;
			base.parameters = parameters;
			recipient = recipientMode.Target;
			sendMode = recipientMode.Mode;
		}

		public AdminMessageRequest(string message, MessageRecipientMode recipientMode)
			: this(message, recipientMode, null)
		{
		}
	}
}