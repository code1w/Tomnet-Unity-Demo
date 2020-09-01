namespace Tom.Entities.Variables
{
	public interface RoomVariable : Variable
	{
		bool IsPrivate
		{
			get;
			set;
		}

		bool IsPersistent
		{
			get;
			set;
		}
	}
}
