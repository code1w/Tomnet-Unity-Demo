using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests.Buddylist
{
    public class InitBuddyListRequest : BaseRequest
    {
        public static readonly string KEY_BLIST = "bl";

        public static readonly string KEY_BUDDY_STATES = "bs";

        public static readonly string KEY_MY_VARS = "mv";

        public InitBuddyListRequest()
            : base(RequestType.InitBuddyList)
        {
        }

        public override void Validate(TomOrange sfs)
        {
            List<string> list = new List<string>();
            if (sfs.BuddyManager.Inited)
            {
                list.Add("Buddy List is already initialized.");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("InitBuddyRequest error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
        }
    }
}
