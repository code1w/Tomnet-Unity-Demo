using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
    public class SubscribeRoomGroupRequest : BaseRequest
    {
        public static readonly string KEY_GROUP_ID = "g";

        public static readonly string KEY_ROOM_LIST = "rl";

        private string groupId;

        public SubscribeRoomGroupRequest(string groupId)
            : base(RequestType.SubscribeRoomGroup)
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
                throw new SFSValidationError("SubscribeGroup request Error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
            sfso.PutUtfString(KEY_GROUP_ID, groupId);
        }
    }
}
