using Tom.Entities;
using Tom.Entities.Data;

namespace Tom.Requests.Buddylist
{
	public class BuddyMessageRequest : GenericMessageRequest
	{
		public BuddyMessageRequest(string message, Buddy targetBuddy, ISFSObject parameters)
		{
			type = 5;
			base.message = message;
			recipient = (targetBuddy?.Id ?? (-1));
			base.parameters = parameters;
		}

		public BuddyMessageRequest(string message, Buddy targetBuddy)
			: this(message, targetBuddy, null)
		{
		}
	}
}
