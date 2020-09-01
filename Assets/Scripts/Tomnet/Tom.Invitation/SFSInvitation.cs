using Tom.Entities.Data;

namespace Tom.Entities.Invitation
{
	public class SFSInvitation : Invitation
	{
		protected int id;

		protected User inviter;

		protected User invitee;

		protected int secondsForAnswer;

		protected ISFSObject parameters;

		public int Id
		{
			get
			{
				return id;
			}
			set
			{
				id = value;
			}
		}

		public User Inviter => inviter;

		public User Invitee => invitee;

		public int SecondsForAnswer => secondsForAnswer;

		public ISFSObject Params => parameters;

		private void Init(User inviter, User invitee, int secondsForAnswer, ISFSObject parameters)
		{
			this.inviter = inviter;
			this.invitee = invitee;
			this.secondsForAnswer = secondsForAnswer;
			this.parameters = parameters;
		}

		public SFSInvitation(User inviter, User invitee)
		{
			Init(inviter, invitee, 15, null);
		}

		public SFSInvitation(User inviter, User invitee, int secondsForAnswer)
		{
			Init(inviter, invitee, secondsForAnswer, null);
		}

		public SFSInvitation(User inviter, User invitee, int secondsForAnswer, ISFSObject parameters)
		{
			Init(inviter, invitee, secondsForAnswer, parameters);
		}
	}
}
