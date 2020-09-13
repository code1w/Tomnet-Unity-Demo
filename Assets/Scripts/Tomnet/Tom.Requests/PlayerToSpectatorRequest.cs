using Tom.Entities;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
    public class PlayerToSpectatorRequest : BaseRequest
    {
        public static readonly string KEY_ROOM_ID = "r";

        public static readonly string KEY_USER_ID = "u";

        private Room room;

        private void Init(Room targetRoom)
        {
            room = targetRoom;
        }

        public PlayerToSpectatorRequest(Room targetRoom)
            : base(RequestType.PlayerToSpectator)
        {
            Init(targetRoom);
        }

        public PlayerToSpectatorRequest()
            : base(RequestType.PlayerToSpectator)
        {
            Init(null);
        }

        public override void Validate(TomOrange sfs)
        {
            List<string> list = new List<string>();
            if (sfs.JoinedRooms.Count < 1)
            {
                list.Add("You are not joined in any rooms");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("LeaveRoom request error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
            if (room == null)
            {
                room = sfs.LastJoinedRoom;
            }
            sfso.PutInt(KEY_ROOM_ID, room.Id);
        }
    }
}
