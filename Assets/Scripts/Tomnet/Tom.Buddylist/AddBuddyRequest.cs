using Tom.Entities;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests.Buddylist
{
	public class AddBuddyRequest : BaseRequest
	{
		public static readonly string KEY_BUDDY_NAME = "bn";

		private string name;

		public AddBuddyRequest(string buddyName)
			: base(RequestType.AddBuddy)
		{
			name = buddyName;
		}

		public override void Validate(TomOrange sfs)
		{
			List<string> list = new List<string>();
			if (!sfs.BuddyManager.Inited)
			{
				list.Add("BuddyList is not inited. Please send an InitBuddyRequest first.");
			}
			if (name == null || name.Length < 1)
			{
				list.Add("Invalid buddy name: " + name);
			}
			if (!sfs.BuddyManager.MyOnlineState)
			{
				list.Add("Can't add buddy while off-line");
			}
			Buddy buddyByName = sfs.BuddyManager.GetBuddyByName(name);
			if (buddyByName != null && !buddyByName.IsTemp)
			{
				list.Add("Can't add buddy, it is already in your list: " + name);
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("BuddyList request error", list);
			}
		}

		public override void Execute(TomOrange sfs)
		{
			sfso.PutUtfString(KEY_BUDDY_NAME, name);
		}
	}
}
