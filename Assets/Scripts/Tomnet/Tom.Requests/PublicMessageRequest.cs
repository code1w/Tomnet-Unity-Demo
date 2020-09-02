using Tom.Entities;
using Tom.Entities.Data;

namespace Tom.Requests
{
	public class PublicMessageRequest : GenericMessageRequest
	{
		public PublicMessageRequest(string message, ISFSObject parameters, Room targetRoom)
		{
			type = 0;
			base.message = message;
			room = targetRoom;
			base.parameters = parameters;
		}

		public PublicMessageRequest(string message, ISFSObject parameters)
			: this(message, parameters, null)
		{
		}

		public PublicMessageRequest(string message)
			: this(message, null, null)
		{
		}
	}
}