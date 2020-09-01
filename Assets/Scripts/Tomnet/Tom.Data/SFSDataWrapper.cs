namespace Tom.Entities.Data
{
	public class SFSDataWrapper
	{
		private int type;

		private object data;

		public int Type => type;

		public object Data => data;

		public SFSDataWrapper(int type, object data)
		{
			this.type = type;
			this.data = data;
		}

		public SFSDataWrapper(SFSDataType tp, object data)
		{
			type = (int)tp;
			this.data = data;
		}
	}
}
