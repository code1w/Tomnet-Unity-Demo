using Tom.Entities;
using Tom.Exceptions;

namespace Tom.Requests
{
    public class LeaveRoomRequest : BaseRequest
    {
        public static readonly string KEY_ROOM_ID = "r";

        private Room room;

        private void Init(Room room)
        {
            this.room = room;
        }

        public LeaveRoomRequest(Room room)
            : base(RequestType.LeaveRoom)
        {
            Init(room);
        }

        public LeaveRoomRequest()
            : base(RequestType.LeaveRoom)
        {
            Init(null);
        }

        public override void Validate(TomOrange sfs)
        {
            if (sfs.JoinedRooms.Count < 1)
            {
                throw new SFSValidationError("LeaveRoom request error", new string[1]
                {
                    "You are not joined in any rooms"
                });
            }
        }

        public override void Execute(TomOrange sfs)
        {
            if (room != null)
            {
                sfso.PutInt(KEY_ROOM_ID, room.Id);
            }
        }
    }
}
