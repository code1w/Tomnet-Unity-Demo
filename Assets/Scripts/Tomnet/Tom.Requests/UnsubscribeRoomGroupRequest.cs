using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
	public class UnsubscribeRoomGroupRequest : BaseRequest
	{
		public static readonly string KEY_GROUP_ID = "g";

		private string groupId;

		public UnsubscribeRoomGroupRequest(string groupId)
			: base(RequestType.UnsubscribeRoomGroup)
		{
			this.groupId = groupId;
		}

		public override void Validate(TomOrange sfs)
		{
			List<string> list = new List<string>();
			if (groupId == null || groupId.Length == 0)
			{
				list.Add("Invalid groupId. Must be a string with at least 1 character.");
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("UnsubscribeGroup request Error", list);
			}
		}

		public override void Execute(TomOrange sfs)
		{
			sfso.PutUtfString(KEY_GROUP_ID, groupId);
		}
	}
}
