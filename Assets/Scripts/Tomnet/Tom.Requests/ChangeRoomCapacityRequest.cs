using Tom.Entities;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
	public class ChangeRoomCapacityRequest : BaseRequest
	{
		public static readonly string KEY_ROOM = "r";

		public static readonly string KEY_USER_SIZE = "u";

		public static readonly string KEY_SPEC_SIZE = "s";

		private Room room;

		private int newMaxUsers;

		private int newMaxSpect;

		public ChangeRoomCapacityRequest(Room room, int newMaxUsers, int newMaxSpect)
			: base(RequestType.ChangeRoomCapacity)
		{
			this.room = room;
			this.newMaxUsers = newMaxUsers;
			this.newMaxSpect = newMaxSpect;
		}

		public override void Validate(TomOrange sfs)
		{
			List<string> list = new List<string>();
			if (room == null)
			{
				list.Add("Provided room is null");
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("ChangeRoomCapacity request error", list);
			}
		}

		public override void Execute(TomOrange sfs)
		{
			sfso.PutInt(KEY_ROOM, room.Id);
			sfso.PutInt(KEY_USER_SIZE, newMaxUsers);
			sfso.PutInt(KEY_SPEC_SIZE, newMaxSpect);
		}
	}
}
