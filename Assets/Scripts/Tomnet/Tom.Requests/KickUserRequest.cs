using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
	public class KickUserRequest : BaseRequest
	{
		public static readonly string KEY_USER_ID = "u";

		public static readonly string KEY_MESSAGE = "m";

		public static readonly string KEY_DELAY = "d";

		private int userId;

		private string message;

		private int delay;

		private void Init(int userId, string message, int delaySeconds)
		{
			this.userId = userId;
			this.message = message;
			delay = delaySeconds;
			if (delay < 0)
			{
				delay = 0;
			}
		}

		public KickUserRequest(int userId, string message, int delaySeconds)
			: base(RequestType.BanUser)
		{
			Init(userId, message, delaySeconds);
		}

		public KickUserRequest(int userId)
			: base(RequestType.KickUser)
		{
			Init(userId, null, 5);
		}

		public KickUserRequest(int userId, string message)
			: base(RequestType.KickUser)
		{
			Init(userId, message, 5);
		}

		public override void Validate(TomOrange sfs)
		{
			List<string> list = new List<string>();
			if (list.Count > 0)
			{
				throw new SFSValidationError("KickUser request error", list);
			}
		}

		public override void Execute(TomOrange sfs)
		{
			sfso.PutInt(KEY_USER_ID, userId);
			sfso.PutInt(KEY_DELAY, delay);
			if (message != null && message.Length > 0)
			{
				sfso.PutUtfString(KEY_MESSAGE, message);
			}
		}
	}
}
