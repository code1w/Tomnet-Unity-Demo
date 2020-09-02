namespace Tom.Requests
{
	public enum RequestType
	{
		Handshake = 0,
		Login = 1,
		Logout = 2,
		GetRoomList = 3,
		JoinRoom = 4,
		AutoJoin = 5,
		CreateRoom = 6,
		GenericMessage = 7,
		ChangeRoomName = 8,
		ChangeRoomPassword = 9,
		ObjectMessage = 10,
		SetRoomVariables = 11,
		SetUserVariables = 12,
		CallExtension = 13,
		LeaveRoom = 14,
		SubscribeRoomGroup = 0xF,
		UnsubscribeRoomGroup = 0x10,
		SpectatorToPlayer = 17,
		PlayerToSpectator = 18,
		ChangeRoomCapacity = 19,
		PublicMessage = 20,
		PrivateMessage = 21,
		ModeratorMessage = 22,
		AdminMessage = 23,
		KickUser = 24,
		BanUser = 25,
		ManualDisconnection = 26,
		FindRooms = 27,
		FindUsers = 28,
		PingPong = 29,
		SetUserPosition = 30,
		InitBuddyList = 200,
		AddBuddy = 201,
		BlockBuddy = 202,
		RemoveBuddy = 203,
		SetBuddyVariables = 204,
		GoOnline = 205,
		InviteUser = 300,
		InvitationReply = 301,
		CreateSFSGame = 302,
		QuickJoinGame = 303,
		JoinRoomInvite = 304
	}
}