using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests.Buddylist
{
    public class GoOnlineRequest : BaseRequest
    {
        public static readonly string KEY_ONLINE = "o";

        public static readonly string KEY_BUDDY_NAME = "bn";

        public static readonly string KEY_BUDDY_ID = "bi";

        private bool online;

        public GoOnlineRequest(bool online)
            : base(RequestType.GoOnline)
        {
            this.online = online;
        }

        public override void Validate(TomOrange sfs)
        {
            List<string> list = new List<string>();
            if (!sfs.BuddyManager.Inited)
            {
                list.Add("BuddyList is not inited. Please send an InitBuddyRequest first.");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("GoOnline request error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
            sfs.BuddyManager.MyOnlineState = online;
            sfso.PutBool(KEY_ONLINE, online);
        }
    }
}
