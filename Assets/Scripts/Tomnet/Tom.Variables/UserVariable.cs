namespace Tom.Entities.Variables
{
	public interface UserVariable : Variable
	{
		bool IsPrivate
		{
			get;
			set;
		}
	}
}
