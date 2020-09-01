namespace Tom.Requests
{
	public class PingPongRequest : BaseRequest
	{
		public PingPongRequest()
			: base(RequestType.PingPong)
		{
		}

		public override void Validate(TomOrange sfs)
		{
		}

		public override void Execute(TomOrange sfs)
		{
		}
	}
}
