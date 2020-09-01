namespace Tom.Bitswarm
{
	public enum PacketReadTransition
	{
		HeaderReceived,
		SizeReceived,
		IncompleteSize,
		WholeSizeReceived,
		PacketFinished,
		InvalidData,
		InvalidDataFinished
	}
}
