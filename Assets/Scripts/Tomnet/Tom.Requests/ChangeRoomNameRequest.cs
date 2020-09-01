using Tom.Entities;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
	public class ChangeRoomNameRequest : BaseRequest
	{
		public static readonly string KEY_ROOM = "r";

		public static readonly string KEY_NAME = "n";

		private Room room;

		private string newName;

		public ChangeRoomNameRequest(Room room, string newName)
			: base(RequestType.ChangeRoomName)
		{
			this.room = room;
			this.newName = newName;
		}

		public override void Validate(TomOrange sfs)
		{
			List<string> list = new List<string>();
			if (room == null)
			{
				list.Add("Provided room is null");
			}
			if (newName == null || newName.Length == 0)
			{
				list.Add("Invalid new room name. It must be a non-null and non-empty string.");
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("ChangeRoomName request error", list);
			}
		}

		public override void Execute(TomOrange sfs)
		{
			sfso.PutInt(KEY_ROOM, room.Id);
			sfso.PutUtfString(KEY_NAME, newName);
		}
	}
}
