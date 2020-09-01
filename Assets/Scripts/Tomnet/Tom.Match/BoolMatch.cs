namespace Tom.Entities.Match
{
	public class BoolMatch : IMatcher
	{
		private static readonly int TYPE_ID = 0;

		public static readonly BoolMatch EQUALS = new BoolMatch("==");

		public static readonly BoolMatch NOT_EQUALS = new BoolMatch("!=");

		private string symbol;

		public string Symbol => symbol;

		public int Type => TYPE_ID;

		public BoolMatch(string symbol)
		{
			this.symbol = symbol;
		}
	}
}
