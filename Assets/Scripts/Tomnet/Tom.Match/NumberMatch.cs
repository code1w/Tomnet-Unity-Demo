namespace Tom.Entities.Match
{
    public class NumberMatch : IMatcher
    {
        private static readonly int TYPE_ID = 1;

        public static readonly NumberMatch EQUALS = new NumberMatch("==");

        public static readonly NumberMatch NOT_EQUALS = new NumberMatch("!=");

        public static readonly NumberMatch GREATER_THAN = new NumberMatch(">");

        public static readonly NumberMatch GREATER_OR_EQUAL_THAN = new NumberMatch(">=");

        public static readonly NumberMatch LESS_THAN = new NumberMatch("<");

        public static readonly NumberMatch LESS_OR_EQUAL_THAN = new NumberMatch("<=");

        private string symbol;

        public string Symbol => symbol;

        public int Type => TYPE_ID;

        public NumberMatch(string symbol)
        {
            this.symbol = symbol;
        }
    }
}
