using Tom.Entities;
using Tom.Entities.Data;
using Tom.Entities.Variables;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
    public class SetRoomVariablesRequest : BaseRequest
    {
        public static readonly string KEY_VAR_ROOM = "r";

        public static readonly string KEY_VAR_LIST = "vl";

        private ICollection<RoomVariable> roomVariables;

        private Room room;

        private void Init(ICollection<RoomVariable> roomVariables, Room room)
        {
            this.roomVariables = roomVariables;
            this.room = room;
        }

        public SetRoomVariablesRequest(ICollection<RoomVariable> roomVariables, Room room)
            : base(RequestType.SetRoomVariables)
        {
            Init(roomVariables, room);
        }

        public SetRoomVariablesRequest(ICollection<RoomVariable> roomVariables)
            : base(RequestType.SetRoomVariables)
        {
            Init(roomVariables, null);
        }

        public override void Validate(TomOrange sfs)
        {
            List<string> list = new List<string>();
            if (room != null)
            {
                if (!room.ContainsUser(sfs.MySelf))
                {
                    list.Add("You are not joined in the target room");
                }
            }
            else if (sfs.LastJoinedRoom == null)
            {
                list.Add("You are not joined in any rooms");
            }
            if (roomVariables == null || roomVariables.Count == 0)
            {
                list.Add("No variables were specified");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("SetRoomVariables request error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
            ISFSArray iSFSArray = SFSArray.NewInstance();
            foreach (RoomVariable roomVariable in roomVariables)
            {
                iSFSArray.AddSFSArray(roomVariable.ToSFSArray());
            }
            if (room == null)
            {
                room = sfs.LastJoinedRoom;
            }
            sfso.PutSFSArray(KEY_VAR_LIST, iSFSArray);
            sfso.PutInt(KEY_VAR_ROOM, room.Id);
        }
    }
}
