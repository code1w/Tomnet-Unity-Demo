using Tom.Entities.Data;
using Tom.Entities.Variables;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests.Buddylist
{
	public class SetBuddyVariablesRequest : BaseRequest
	{
		public static readonly string KEY_BUDDY_NAME = "bn";

		public static readonly string KEY_BUDDY_VARS = "bv";

		private List<BuddyVariable> buddyVariables;

		public SetBuddyVariablesRequest(List<BuddyVariable> buddyVariables)
			: base(RequestType.SetBuddyVariables)
		{
			this.buddyVariables = buddyVariables;
		}

		public override void Validate(TomOrange sfs)
		{
			List<string> list = new List<string>();
			if (!sfs.BuddyManager.Inited)
			{
				list.Add("BuddyList is not inited. Please send an InitBuddyRequest first.");
			}
			if (!sfs.BuddyManager.MyOnlineState)
			{
				list.Add("Can't set buddy variables while off-line");
			}
			if (buddyVariables == null || buddyVariables.Count == 0)
			{
				list.Add("No variables were specified");
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("SetBuddyVariables request error", list);
			}
		}

		public override void Execute(TomOrange sfs)
		{
			ISFSArray iSFSArray = SFSArray.NewInstance();
			foreach (BuddyVariable buddyVariable in buddyVariables)
			{
				iSFSArray.AddSFSArray(buddyVariable.ToSFSArray());
			}
			sfso.PutSFSArray(KEY_BUDDY_VARS, iSFSArray);
		}
	}
}
