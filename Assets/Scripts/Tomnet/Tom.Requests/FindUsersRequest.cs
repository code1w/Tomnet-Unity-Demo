using Tom.Entities;
using Tom.Entities.Match;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
	public class FindUsersRequest : BaseRequest
	{
		public static readonly string KEY_EXPRESSION = "e";

		public static readonly string KEY_GROUP = "g";

		public static readonly string KEY_ROOM = "r";

		public static readonly string KEY_LIMIT = "l";

		public static readonly string KEY_FILTERED_USERS = "fu";

		private MatchExpression matchExpr;

		private object target;

		private int limit;

		private void Init(MatchExpression expr, object target, int limit)
		{
			matchExpr = expr;
			this.target = target;
			this.limit = limit;
		}

		public FindUsersRequest(MatchExpression expr, string target, int limit)
			: base(RequestType.FindUsers)
		{
			Init(expr, target, limit);
		}

		public FindUsersRequest(MatchExpression expr)
			: base(RequestType.FindUsers)
		{
			Init(expr, null, 0);
		}

		public FindUsersRequest(MatchExpression expr, Room target)
			: base(RequestType.FindUsers)
		{
			Init(expr, target, 0);
		}

		public FindUsersRequest(MatchExpression expr, Room target, int limit)
			: base(RequestType.FindUsers)
		{
			Init(expr, target, limit);
		}

		public FindUsersRequest(MatchExpression expr, string target)
			: base(RequestType.FindUsers)
		{
			Init(expr, target, 0);
		}

		public override void Validate(TomOrange sfs)
		{
			List<string> list = new List<string>();
			if (matchExpr == null)
			{
				list.Add("Missing Match Expression");
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("FindUsers request error", list);
			}
		}

		public override void Execute(TomOrange sfs)
		{
			sfso.PutSFSArray(KEY_EXPRESSION, matchExpr.ToSFSArray());
			if (target != null)
			{
				if (target is Room)
				{
					sfso.PutInt(KEY_ROOM, (target as Room).Id);
				}
				else if (target is string)
				{
					sfso.PutUtfString(KEY_GROUP, target as string);
				}
				else
				{
					sfs.Log.Warn("Unsupport target type for FindUsersRequest: " + target);
				}
			}
			if (limit > 0)
			{
				sfso.PutShort(KEY_LIMIT, (short)limit);
			}
		}
	}
}
