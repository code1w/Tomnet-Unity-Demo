using Tom.Entities.Data;

namespace Tom.Requests
{
	public class PrivateMessageRequest : GenericMessageRequest
	{
		public PrivateMessageRequest(string message, int recipientId, ISFSObject parameters)
		{
			type = 1;
			base.message = message;
			recipient = recipientId;
			base.parameters = parameters;
		}

		public PrivateMessageRequest(string message, int recipientId)
			: this(message, recipientId, null)
		{
		}
	}
}
