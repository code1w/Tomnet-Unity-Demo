namespace Tom.Requests
{
	public class ManualDisconnectionRequest : BaseRequest
	{
		public ManualDisconnectionRequest()
			: base(RequestType.ManualDisconnection)
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
