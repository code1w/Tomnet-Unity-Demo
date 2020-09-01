using Tom.Bitswarm;
using Tom.Core;
using Tom.Entities;
using Tom.Entities.Data;
using Tom.Entities.Invitation;
using Tom.Entities.Managers;
using Tom.Entities.Variables;
using Tom.Logging;
using Tom.Requests;
using Tom.Requests.Buddylist;
using Tom.Requests.Game;
using Tom.Requests.MMO;
using Tom.Util;
using System.Collections.Generic;

namespace Tom.Controllers
{
	public class SystemController : BaseController
	{
		private Dictionary<int, RequestDelegate> requestHandlers;

		public SystemController(ISocketClient socketClient)
			: base(socketClient)
		{
			requestHandlers = new Dictionary<int, RequestDelegate>();
			InitRequestHandlers();
		}

		private void InitRequestHandlers()
		{
			requestHandlers[0] = FnHandshake;
			requestHandlers[1] = FnLogin;
			requestHandlers[2] = FnLogout;
			requestHandlers[4] = FnJoinRoom;
			requestHandlers[6] = FnCreateRoom;
			requestHandlers[7] = FnGenericMessage;
			requestHandlers[8] = FnChangeRoomName;
			requestHandlers[9] = FnChangeRoomPassword;
			requestHandlers[19] = FnChangeRoomCapacity;
			requestHandlers[11] = FnSetRoomVariables;
			requestHandlers[12] = FnSetUserVariables;
			requestHandlers[15] = FnSubscribeRoomGroup;
			requestHandlers[16] = FnUnsubscribeRoomGroup;
			requestHandlers[17] = FnSpectatorToPlayer;
			requestHandlers[18] = FnPlayerToSpectator;
			requestHandlers[200] = FnInitBuddyList;
			requestHandlers[201] = FnAddBuddy;
			requestHandlers[203] = FnRemoveBuddy;
			requestHandlers[202] = FnBlockBuddy;
			requestHandlers[205] = FnGoOnline;
			requestHandlers[204] = FnSetBuddyVariables;
			requestHandlers[27] = FnFindRooms;
			requestHandlers[28] = FnFindUsers;
			requestHandlers[300] = FnInviteUsers;
			requestHandlers[301] = FnInvitationReply;
			requestHandlers[303] = FnQuickJoinGame;
			requestHandlers[29] = FnPingPong;
			requestHandlers[30] = FnSetUserPosition;
			requestHandlers[1000] = FnUserEnterRoom;
			requestHandlers[1001] = FnUserCountChange;
			requestHandlers[1002] = FnUserLost;
			requestHandlers[1003] = FnRoomLost;
			requestHandlers[1004] = FnUserExitRoom;
			requestHandlers[1005] = FnClientDisconnection;
			requestHandlers[1006] = FnReconnectionFailure;
			requestHandlers[1007] = FnSetMMOItemVariables;
		}

		public override void HandleMessage(IMessage message)
		{
			if (sfs.Debug)
			{
				log.Info(string.Concat("Message: ", (RequestType)message.Id, " ", message));
			}
			if (!requestHandlers.ContainsKey(message.Id))
			{
				log.Warn("Unknown message id: " + message.Id);
			}
			else
			{
				RequestDelegate requestDelegate = requestHandlers[message.Id];
				requestDelegate(message);
			}
		}

		private void FnHandshake(IMessage msg)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["message"] = msg.Content;
			SFSEvent evt = new SFSEvent(SFSEvent.HANDSHAKE, dictionary);
			sfs.HandleHandShake(evt);
			sfs.DispatchEvent(evt);
		}

		private void FnLogin(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				PopulateRoomList(content.GetSFSArray(LoginRequest.KEY_ROOMLIST));
				sfs.MySelf = new SFSUser(content.GetInt(LoginRequest.KEY_ID), content.GetUtfString(LoginRequest.KEY_USER_NAME), isItMe: true);
				sfs.MySelf.UserManager = sfs.UserManager;
				sfs.MySelf.PrivilegeId = content.GetShort(LoginRequest.KEY_PRIVILEGE_ID);
				sfs.UserManager.AddUser(sfs.MySelf);
				sfs.SetReconnectionSeconds(content.GetShort(LoginRequest.KEY_RECONNECTION_SECONDS));
				sfs.MySelf.PrivilegeId = content.GetShort(LoginRequest.KEY_PRIVILEGE_ID);
				dictionary["zone"] = content.GetUtfString(LoginRequest.KEY_ZONE_NAME);
				dictionary["user"] = sfs.MySelf;
				dictionary["data"] = content.GetSFSObject(LoginRequest.KEY_PARAMS);
				SFSEvent evt = new SFSEvent(SFSEvent.LOGIN, dictionary);
				sfs.HandleLogin(evt);
				sfs.DispatchEvent(evt);
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.LOGIN_ERROR, dictionary));
			}
		}

		private void FnCreateRoom(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				IRoomManager roomManager = sfs.RoomManager;
				Room room = SFSRoom.FromSFSArray(content.GetSFSArray(CreateRoomRequest.KEY_ROOM));
				room.RoomManager = sfs.RoomManager;
				roomManager.AddRoom(room);
				dictionary["room"] = room;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_ADD, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_CREATION_ERROR, dictionary));
			}
		}

		private void FnJoinRoom(IMessage msg)
		{
			IRoomManager roomManager = sfs.RoomManager;
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			sfs.IsJoining = false;
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				ISFSArray sFSArray = content.GetSFSArray(JoinRoomRequest.KEY_ROOM);
				ISFSArray sFSArray2 = content.GetSFSArray(JoinRoomRequest.KEY_USER_LIST);
				Room room = SFSRoom.FromSFSArray(sFSArray);
				room.RoomManager = sfs.RoomManager;
				room = roomManager.ReplaceRoom(room, roomManager.ContainsGroup(room.GroupId));
				for (int i = 0; i < sFSArray2.Size(); i++)
				{
					ISFSArray sFSArray3 = sFSArray2.GetSFSArray(i);
					User orCreateUser = GetOrCreateUser(sFSArray3, addToGlobalManager: true, room);
					orCreateUser.SetPlayerId(sFSArray3.GetShort(3), room);
					room.AddUser(orCreateUser);
				}
				room.IsJoined = true;
				sfs.LastJoinedRoom = room;
				dictionary["room"] = room;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_JOIN, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_JOIN_ERROR, dictionary));
			}
		}

		private void FnUserEnterRoom(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			Room roomById = sfs.RoomManager.GetRoomById(content.GetInt("r"));
			if (roomById != null)
			{
				ISFSArray sFSArray = content.GetSFSArray("u");
				User orCreateUser = GetOrCreateUser(sFSArray, addToGlobalManager: true, roomById);
				roomById.AddUser(orCreateUser);
				dictionary["user"] = orCreateUser;
				dictionary["room"] = roomById;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.USER_ENTER_ROOM, dictionary));
			}
		}

		private void FnUserCountChange(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			Room roomById = sfs.RoomManager.GetRoomById(content.GetInt("r"));
			if (roomById != null)
			{
				int @short = content.GetShort("uc");
				int num = 0;
				if (content.ContainsKey("sc"))
				{
					num = content.GetShort("sc");
				}
				roomById.UserCount = @short;
				roomById.SpectatorCount = num;
				dictionary["room"] = roomById;
				dictionary["uCount"] = @short;
				dictionary["sCount"] = num;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.USER_COUNT_CHANGE, dictionary));
			}
		}

		private void FnUserLost(IMessage msg)
		{
			ISFSObject content = msg.Content;
			int @int = content.GetInt("u");
			User userById = sfs.UserManager.GetUserById(@int);
			if (userById == null)
			{
				return;
			}
			List<Room> userRooms = sfs.RoomManager.GetUserRooms(userById);
			sfs.RoomManager.RemoveUser(userById);
			SFSGlobalUserManager sFSGlobalUserManager = sfs.UserManager as SFSGlobalUserManager;
			sFSGlobalUserManager.RemoveUserReference(userById, disconnected: true);
			foreach (Room item in userRooms)
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary["user"] = userById;
				dictionary["room"] = item;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.USER_EXIT_ROOM, dictionary));
			}
		}

		private void FnRoomLost(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = content.GetInt("r");
			Room roomById = sfs.RoomManager.GetRoomById(@int);
			IUserManager userManager = sfs.UserManager;
			if (roomById == null)
			{
				return;
			}
			sfs.RoomManager.RemoveRoom(roomById);
			foreach (User user in roomById.UserList)
			{
				userManager.RemoveUser(user);
			}
			dictionary["room"] = roomById;
			sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_REMOVE, dictionary));
		}

		private void FnUserExitRoom(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = content.GetInt("r");
			int int2 = content.GetInt("u");
			Room roomById = sfs.RoomManager.GetRoomById(@int);
			User userById = sfs.UserManager.GetUserById(int2);
			if (roomById != null && userById != null)
			{
				roomById.RemoveUser(userById);
				sfs.UserManager.RemoveUser(userById);
				if (userById.IsItMe && roomById.IsJoined)
				{
					roomById.IsJoined = false;
					if (sfs.JoinedRooms.Count == 0)
					{
						sfs.LastJoinedRoom = null;
					}
					if (!roomById.IsManaged)
					{
						sfs.RoomManager.RemoveRoom(roomById);
					}
				}
				dictionary["user"] = userById;
				dictionary["room"] = roomById;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.USER_EXIT_ROOM, dictionary));
			}
			else
			{
				log.Debug(string.Concat("Failed to handle UserExit event. Room: ", roomById, ", User: ", userById));
			}
		}

		private void FnClientDisconnection(IMessage msg)
		{
			ISFSObject content = msg.Content;
			int @byte = content.GetByte("dr");
			string reason = ClientDisconnectionReason.GetReason(@byte);
			sfs.HandleClientDisconnection(reason);
		}

		private void FnSetRoomVariables(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = content.GetInt(SetRoomVariablesRequest.KEY_VAR_ROOM);
			ISFSArray sFSArray = content.GetSFSArray(SetRoomVariablesRequest.KEY_VAR_LIST);
			Room roomById = sfs.RoomManager.GetRoomById(@int);
			List<string> list = new List<string>();
			if (roomById != null)
			{
				for (int i = 0; i < sFSArray.Size(); i++)
				{
					RoomVariable roomVariable = SFSRoomVariable.FromSFSArray(sFSArray.GetSFSArray(i));
					roomById.SetVariable(roomVariable);
					list.Add(roomVariable.Name);
				}
				dictionary["changedVars"] = list;
				dictionary["room"] = roomById;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_VARIABLES_UPDATE, dictionary));
			}
			else
			{
				log.Warn("RoomVariablesUpdate, unknown Room id = " + @int);
			}
		}

		private void FnSetUserVariables(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = content.GetInt(SetUserVariablesRequest.KEY_USER);
			ISFSArray sFSArray = content.GetSFSArray(SetUserVariablesRequest.KEY_VAR_LIST);
			User userById = sfs.UserManager.GetUserById(@int);
			List<string> list = new List<string>();
			if (userById != null)
			{
				for (int i = 0; i < sFSArray.Size(); i++)
				{
					UserVariable userVariable = SFSUserVariable.FromSFSArray(sFSArray.GetSFSArray(i));
					userById.SetVariable(userVariable);
					list.Add(userVariable.Name);
				}
				dictionary["changedVars"] = list;
				dictionary["user"] = userById;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.USER_VARIABLES_UPDATE, dictionary));
			}
			else
			{
				log.Warn("UserVariablesUpdate: unknown user id = " + @int);
			}
		}

		private void FnSubscribeRoomGroup(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				string utfString = content.GetUtfString(SubscribeRoomGroupRequest.KEY_GROUP_ID);
				ISFSArray sFSArray = content.GetSFSArray(SubscribeRoomGroupRequest.KEY_ROOM_LIST);
				if (sfs.RoomManager.ContainsGroup(utfString))
				{
					base.log.Warn("SubscribeGroup Error. Group: " + utfString + " already subscribed!");
				}
				sfs.RoomManager.AddGroup(utfString);
				PopulateRoomList(sFSArray);
				dictionary["groupId"] = utfString;
				dictionary["newRooms"] = sfs.RoomManager.GetRoomListFromGroup(utfString);
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_GROUP_SUBSCRIBE, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_GROUP_SUBSCRIBE_ERROR, dictionary));
			}
		}

		private void FnUnsubscribeRoomGroup(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				string utfString = content.GetUtfString(UnsubscribeRoomGroupRequest.KEY_GROUP_ID);
				if (!sfs.RoomManager.ContainsGroup(utfString))
				{
					base.log.Warn("UnsubscribeGroup Error. Group:" + utfString + "is not subscribed!");
				}
				sfs.RoomManager.RemoveGroup(utfString);
				dictionary["groupId"] = utfString;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_GROUP_UNSUBSCRIBE, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_GROUP_UNSUBSCRIBE_ERROR, dictionary));
			}
		}

		private void FnChangeRoomName(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				int @int = content.GetInt(ChangeRoomNameRequest.KEY_ROOM);
				Room roomById = sfs.RoomManager.GetRoomById(@int);
				if (roomById != null)
				{
					dictionary["oldName"] = roomById.Name;
					sfs.RoomManager.ChangeRoomName(roomById, content.GetUtfString(ChangeRoomNameRequest.KEY_NAME));
					dictionary["room"] = roomById;
					sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_NAME_CHANGE, dictionary));
				}
				else
				{
					base.log.Warn("Room not found, ID:" + @int + ", Room name change failed.");
				}
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_NAME_CHANGE_ERROR, dictionary));
			}
		}

		private void FnChangeRoomPassword(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				int @int = content.GetInt(ChangeRoomPasswordStateRequest.KEY_ROOM);
				Room roomById = sfs.RoomManager.GetRoomById(@int);
				if (roomById != null)
				{
					sfs.RoomManager.ChangeRoomPasswordState(roomById, content.GetBool(ChangeRoomPasswordStateRequest.KEY_PASS));
					dictionary["room"] = roomById;
					sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_PASSWORD_STATE_CHANGE, dictionary));
				}
				else
				{
					base.log.Warn("Room not found, ID:" + @int + ", Room password change failed.");
				}
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_PASSWORD_STATE_CHANGE_ERROR, dictionary));
			}
		}

		private void FnChangeRoomCapacity(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				int @int = content.GetInt(ChangeRoomCapacityRequest.KEY_ROOM);
				Room roomById = sfs.RoomManager.GetRoomById(@int);
				if (roomById != null)
				{
					sfs.RoomManager.ChangeRoomCapacity(roomById, content.GetInt(ChangeRoomCapacityRequest.KEY_USER_SIZE), content.GetInt(ChangeRoomCapacityRequest.KEY_SPEC_SIZE));
					dictionary["room"] = roomById;
					sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_CAPACITY_CHANGE, dictionary));
				}
				else
				{
					base.log.Warn("Room not found, ID:" + @int + ", Room capacity change failed.");
				}
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_CAPACITY_CHANGE_ERROR, dictionary));
			}
		}

		private void FnLogout(IMessage msg)
		{
			sfs.HandleLogout();
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["zoneName"] = content.GetUtfString(LogoutRequest.KEY_ZONE_NAME);
			sfs.DispatchEvent(new SFSEvent(SFSEvent.LOGOUT, dictionary));
		}

		private User GetOrCreateUser(ISFSArray userObj)
		{
			return GetOrCreateUser(userObj, addToGlobalManager: false, null);
		}

		private User GetOrCreateUser(ISFSArray userObj, bool addToGlobalManager)
		{
			return GetOrCreateUser(userObj, addToGlobalManager, null);
		}

		private User GetOrCreateUser(ISFSArray userObj, bool addToGlobalManager, Room room)
		{
			int @int = userObj.GetInt(0);
			User user = sfs.UserManager.GetUserById(@int);
			if (user == null)
			{
				user = SFSUser.FromSFSArray(userObj, room);
				user.UserManager = sfs.UserManager;
			}
			else if (room != null)
			{
				user.SetPlayerId(userObj.GetShort(3), room);
				ISFSArray sFSArray = userObj.GetSFSArray(4);
				for (int i = 0; i < sFSArray.Size(); i++)
				{
					user.SetVariable(SFSUserVariable.FromSFSArray(sFSArray.GetSFSArray(i)));
				}
			}
			if (addToGlobalManager)
			{
				sfs.UserManager.AddUser(user);
			}
			return user;
		}

		private void FnSpectatorToPlayer(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				int @int = content.GetInt(SpectatorToPlayerRequest.KEY_ROOM_ID);
				int int2 = content.GetInt(SpectatorToPlayerRequest.KEY_USER_ID);
				int @short = content.GetShort(SpectatorToPlayerRequest.KEY_PLAYER_ID);
				User userById = sfs.UserManager.GetUserById(int2);
				Room roomById = sfs.RoomManager.GetRoomById(@int);
				if (roomById != null)
				{
					if (userById != null)
					{
						if (userById.IsJoinedInRoom(roomById))
						{
							userById.SetPlayerId(@short, roomById);
							dictionary["room"] = roomById;
							dictionary["user"] = userById;
							dictionary["playerId"] = @short;
							sfs.DispatchEvent(new SFSEvent(SFSEvent.SPECTATOR_TO_PLAYER, dictionary));
						}
						else
						{
							base.log.Warn(string.Concat("User: ", userById, " not joined in Room: ", roomById, ", SpectatorToPlayer failed."));
						}
					}
					else
					{
						base.log.Warn("User not found, ID:" + int2 + ", SpectatorToPlayer failed.");
					}
				}
				else
				{
					base.log.Warn("Room not found, ID:" + @int + ", SpectatorToPlayer failed.");
				}
			}
			else
			{
				short short2 = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(short2, log, utfStringArray));
				dictionary["errorCode"] = short2;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.SPECTATOR_TO_PLAYER_ERROR, dictionary));
			}
		}

		private void FnPlayerToSpectator(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				int @int = content.GetInt(PlayerToSpectatorRequest.KEY_ROOM_ID);
				int int2 = content.GetInt(PlayerToSpectatorRequest.KEY_USER_ID);
				User userById = sfs.UserManager.GetUserById(int2);
				Room roomById = sfs.RoomManager.GetRoomById(@int);
				if (roomById != null)
				{
					if (userById != null)
					{
						if (userById.IsJoinedInRoom(roomById))
						{
							userById.SetPlayerId(-1, roomById);
							dictionary["room"] = roomById;
							dictionary["user"] = userById;
							sfs.DispatchEvent(new SFSEvent(SFSEvent.PLAYER_TO_SPECTATOR, dictionary));
						}
						else
						{
							base.log.Warn(string.Concat("User: ", userById, " not joined in Room: ", roomById, ", PlayerToSpectator failed."));
						}
					}
					else
					{
						base.log.Warn("User not found, ID:" + int2 + ", PlayerToSpectator failed.");
					}
				}
				else
				{
					base.log.Warn("Room not found, ID:" + @int + ", PlayerToSpectator failed.");
				}
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.PLAYER_TO_SPECTATOR_ERROR, dictionary));
			}
		}

		private void FnInitBuddyList(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				ISFSArray sFSArray = content.GetSFSArray(InitBuddyListRequest.KEY_BLIST);
				ISFSArray sFSArray2 = content.GetSFSArray(InitBuddyListRequest.KEY_MY_VARS);
				string[] utfStringArray = content.GetUtfStringArray(InitBuddyListRequest.KEY_BUDDY_STATES);
				sfs.BuddyManager.ClearAll();
				for (int i = 0; i < sFSArray.Size(); i++)
				{
					Buddy buddy = SFSBuddy.FromSFSArray(sFSArray.GetSFSArray(i));
					sfs.BuddyManager.AddBuddy(buddy);
				}
				if (utfStringArray != null)
				{
					sfs.BuddyManager.BuddyStates = new List<string>(utfStringArray);
				}
				List<BuddyVariable> list = new List<BuddyVariable>();
				for (int j = 0; j < sFSArray2.Size(); j++)
				{
					list.Add(SFSBuddyVariable.FromSFSArray(sFSArray2.GetSFSArray(j)));
				}
				sfs.BuddyManager.MyVariables = list;
				sfs.BuddyManager.Inited = true;
				dictionary["buddyList"] = sfs.BuddyManager.BuddyList;
				dictionary["myVariables"] = sfs.BuddyManager.MyVariables;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_LIST_INIT, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray2 = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray2));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ERROR, dictionary));
			}
		}

		private void FnAddBuddy(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				Buddy buddy = SFSBuddy.FromSFSArray(content.GetSFSArray(AddBuddyRequest.KEY_BUDDY_NAME));
				sfs.BuddyManager.AddBuddy(buddy);
				dictionary["buddy"] = buddy;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ADD, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ERROR, dictionary));
			}
		}

		private void FnRemoveBuddy(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				string utfString = content.GetUtfString(RemoveBuddyRequest.KEY_BUDDY_NAME);
				Buddy buddy = sfs.BuddyManager.RemoveBuddyByName(utfString);
				if (buddy != null)
				{
					dictionary["buddy"] = buddy;
					sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_REMOVE, dictionary));
				}
				else
				{
					base.log.Warn("RemoveBuddy failed, buddy not found: " + utfString);
				}
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ERROR, dictionary));
			}
		}

		private void FnBlockBuddy(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				string utfString = content.GetUtfString(BlockBuddyRequest.KEY_BUDDY_NAME);
				Buddy buddy = sfs.BuddyManager.GetBuddyByName(utfString);
				if (content.ContainsKey(BlockBuddyRequest.KEY_BUDDY))
				{
					buddy = SFSBuddy.FromSFSArray(content.GetSFSArray(BlockBuddyRequest.KEY_BUDDY));
					sfs.BuddyManager.AddBuddy(buddy);
				}
				else
				{
					if (buddy == null)
					{
						base.log.Warn("BlockBuddy failed, buddy not found: " + utfString + ", in local BuddyList");
						return;
					}
					buddy.IsBlocked = content.GetBool(BlockBuddyRequest.KEY_BUDDY_BLOCK_STATE);
				}
				dictionary["buddy"] = buddy;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_BLOCK, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ERROR, dictionary));
			}
		}

		private void FnGoOnline(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				string utfString = content.GetUtfString(GoOnlineRequest.KEY_BUDDY_NAME);
				Buddy buddyByName = sfs.BuddyManager.GetBuddyByName(utfString);
				bool flag = utfString == sfs.MySelf.Name;
				int @byte = content.GetByte(GoOnlineRequest.KEY_ONLINE);
				bool flag2 = @byte == 0;
				bool flag3 = true;
				if (flag)
				{
					if (sfs.BuddyManager.MyOnlineState != flag2)
					{
						base.log.Warn("Unexpected: MyOnlineState is not in synch with the server. Resynching: " + flag2);
						sfs.BuddyManager.MyOnlineState = flag2;
					}
				}
				else
				{
					if (buddyByName == null)
					{
						base.log.Warn("GoOnline error, buddy not found: " + utfString + ", in local BuddyList.");
						return;
					}
					buddyByName.Id = content.GetInt(GoOnlineRequest.KEY_BUDDY_ID);
					BuddyVariable variable = new SFSBuddyVariable(ReservedBuddyVariables.BV_ONLINE, flag2);
					buddyByName.SetVariable(variable);
					if (@byte == 2)
					{
						buddyByName.ClearVolatileVariables();
					}
					flag3 = sfs.BuddyManager.MyOnlineState;
				}
				if (flag3)
				{
					dictionary["buddy"] = buddyByName;
					dictionary["isItMe"] = flag;
					sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ONLINE_STATE_UPDATE, dictionary));
				}
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ERROR, dictionary));
			}
		}

		private void FnReconnectionFailure(IMessage msg)
		{
			sfs.HandleReconnectionFailure();
		}

		private void FnSetBuddyVariables(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				string utfString = content.GetUtfString(SetBuddyVariablesRequest.KEY_BUDDY_NAME);
				ISFSArray sFSArray = content.GetSFSArray(SetBuddyVariablesRequest.KEY_BUDDY_VARS);
				Buddy buddyByName = sfs.BuddyManager.GetBuddyByName(utfString);
				bool flag = utfString == sfs.MySelf.Name;
				List<string> list = new List<string>();
				List<BuddyVariable> list2 = new List<BuddyVariable>();
				bool flag2 = true;
				for (int i = 0; i < sFSArray.Size(); i++)
				{
					BuddyVariable buddyVariable = SFSBuddyVariable.FromSFSArray(sFSArray.GetSFSArray(i));
					list2.Add(buddyVariable);
					list.Add(buddyVariable.Name);
				}
				if (flag)
				{
					sfs.BuddyManager.MyVariables = list2;
				}
				else
				{
					if (buddyByName == null)
					{
						base.log.Warn("Unexpected. Target of BuddyVariables update not found: " + utfString);
						return;
					}
					buddyByName.SetVariables(list2);
					flag2 = sfs.BuddyManager.MyOnlineState;
				}
				if (flag2)
				{
					dictionary["isItMe"] = flag;
					dictionary["changedVars"] = list;
					dictionary["buddy"] = buddyByName;
					sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_VARIABLES_UPDATE, dictionary));
				}
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_ERROR, dictionary));
			}
		}

		private void FnFindRooms(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			ISFSArray sFSArray = content.GetSFSArray(FindRoomsRequest.KEY_FILTERED_ROOMS);
			List<Room> list = new List<Room>();
			for (int i = 0; i < sFSArray.Size(); i++)
			{
				Room room = SFSRoom.FromSFSArray(sFSArray.GetSFSArray(i));
				Room roomById = sfs.RoomManager.GetRoomById(room.Id);
				if (roomById != null)
				{
					room.IsJoined = roomById.IsJoined;
				}
				list.Add(room);
			}
			dictionary["rooms"] = list;
			sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_FIND_RESULT, dictionary));
		}

		private void FnFindUsers(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			ISFSArray sFSArray = content.GetSFSArray(FindUsersRequest.KEY_FILTERED_USERS);
			List<User> list = new List<User>();
			User mySelf = sfs.MySelf;
			for (int i = 0; i < sFSArray.Size(); i++)
			{
				User user = SFSUser.FromSFSArray(sFSArray.GetSFSArray(i));
				if (user.Id == mySelf.Id)
				{
					user = mySelf;
				}
				list.Add(user);
			}
			dictionary["users"] = list;
			sfs.DispatchEvent(new SFSEvent(SFSEvent.USER_FIND_RESULT, dictionary));
		}

		private void FnInviteUsers(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			User user = null;
			user = ((!content.ContainsKey(InviteUsersRequest.KEY_USER_ID)) ? SFSUser.FromSFSArray(content.GetSFSArray(InviteUsersRequest.KEY_USER)) : sfs.UserManager.GetUserById(content.GetInt(InviteUsersRequest.KEY_USER_ID)));
			int @short = content.GetShort(InviteUsersRequest.KEY_TIME);
			int @int = content.GetInt(InviteUsersRequest.KEY_INVITATION_ID);
			ISFSObject sFSObject = content.GetSFSObject(InviteUsersRequest.KEY_PARAMS);
			Invitation invitation = new SFSInvitation(user, sfs.MySelf, @short, sFSObject);
			invitation.Id = @int;
			dictionary["invitation"] = invitation;
			sfs.DispatchEvent(new SFSEvent(SFSEvent.INVITATION, dictionary));
		}

		private void FnInvitationReply(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.IsNull(BaseRequest.KEY_ERROR_CODE))
			{
				User user = null;
				user = ((!content.ContainsKey(InviteUsersRequest.KEY_USER_ID)) ? SFSUser.FromSFSArray(content.GetSFSArray(InviteUsersRequest.KEY_USER)) : sfs.UserManager.GetUserById(content.GetInt(InviteUsersRequest.KEY_USER_ID)));
				int @byte = content.GetByte(InviteUsersRequest.KEY_REPLY_ID);
				ISFSObject sFSObject = content.GetSFSObject(InviteUsersRequest.KEY_PARAMS);
				dictionary["invitee"] = user;
				dictionary["reply"] = @byte;
				dictionary["data"] = sFSObject;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.INVITATION_REPLY, dictionary));
			}
			else
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.INVITATION_REPLY_ERROR, dictionary));
			}
		}

		private void FnQuickJoinGame(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (content.ContainsKey(BaseRequest.KEY_ERROR_CODE))
			{
				short @short = content.GetShort(BaseRequest.KEY_ERROR_CODE);
				Logger log = sfs.Log;
				object[] utfStringArray = content.GetUtfStringArray(BaseRequest.KEY_ERROR_PARAMS);
				string text = (string)(dictionary["errorMessage"] = SFSErrorCodes.GetErrorMessage(@short, log, utfStringArray));
				dictionary["errorCode"] = @short;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.ROOM_JOIN_ERROR, dictionary));
			}
		}

		private void FnPingPong(IMessage msg)
		{
			int num = sfs.LagMonitor.OnPingPong();
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["lagValue"] = num;
			sfs.DispatchEvent(new SFSEvent(SFSEvent.PING_PONG, dictionary));
		}

		private void FnSetUserPosition(IMessage msg)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			ISFSObject content = msg.Content;
			int @int = content.GetInt(SetUserPositionRequest.KEY_ROOM);
			int[] intArray = content.GetIntArray(SetUserPositionRequest.KEY_MINUS_USER_LIST);
			ISFSArray sFSArray = content.GetSFSArray(SetUserPositionRequest.KEY_PLUS_USER_LIST);
			int[] intArray2 = content.GetIntArray(SetUserPositionRequest.KEY_MINUS_ITEM_LIST);
			ISFSArray sFSArray2 = content.GetSFSArray(SetUserPositionRequest.KEY_PLUS_ITEM_LIST);
			Room roomById = sfs.RoomManager.GetRoomById(@int);
			if (roomById == null)
			{
				return;
			}
			List<User> list = new List<User>();
			List<User> list2 = new List<User>();
			List<IMMOItem> list3 = new List<IMMOItem>();
			List<IMMOItem> list4 = new List<IMMOItem>();
			if (intArray != null && intArray.Length != 0)
			{
				int[] array = intArray;
				foreach (int id in array)
				{
					User userById = roomById.GetUserById(id);
					if (userById != null)
					{
						roomById.RemoveUser(userById);
						list2.Add(userById);
					}
				}
			}
			if (sFSArray != null)
			{
				for (int j = 0; j < sFSArray.Size(); j++)
				{
					ISFSArray sFSArray3 = sFSArray.GetSFSArray(j);
					User orCreateUser = GetOrCreateUser(sFSArray3, addToGlobalManager: true, roomById);
					list.Add(orCreateUser);
					roomById.AddUser(orCreateUser);
					if (sFSArray3.Size() > 5)
					{
						object elementAt = sFSArray3.GetElementAt(5);
						(orCreateUser as SFSUser).AOIEntryPoint = Vec3D.fromArray(elementAt);
					}
				}
			}
			MMORoom mMORoom = roomById as MMORoom;
			if (intArray2 != null)
			{
				int[] array2 = intArray2;
				foreach (int id2 in array2)
				{
					IMMOItem mMOItem = mMORoom.GetMMOItem(id2);
					if (mMOItem != null)
					{
						mMORoom.RemoveItem(id2);
						list4.Add(mMOItem);
					}
				}
			}
			if (sFSArray2 != null)
			{
				for (int l = 0; l < sFSArray2.Size(); l++)
				{
					ISFSArray sFSArray4 = sFSArray2.GetSFSArray(l);
					IMMOItem iMMOItem = MMOItem.FromSFSArray(sFSArray4);
					list3.Add(iMMOItem);
					mMORoom.AddMMOItem(iMMOItem);
					if (sFSArray4.Size() > 2)
					{
						object elementAt2 = sFSArray4.GetElementAt(2);
						(iMMOItem as MMOItem).AOIEntryPoint = Vec3D.fromArray(elementAt2);
					}
				}
			}
			dictionary["addedItems"] = list3;
			dictionary["removedItems"] = list4;
			dictionary["removedUsers"] = list2;
			dictionary["addedUsers"] = list;
			dictionary["room"] = (roomById as MMORoom);
			sfs.DispatchEvent(new SFSEvent(SFSEvent.PROXIMITY_LIST_UPDATE, dictionary));
		}

		private void FnSetMMOItemVariables(IMessage msg)
		{
			ISFSObject content = msg.Content;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = content.GetInt(SetMMOItemVariables.KEY_ROOM_ID);
			int int2 = content.GetInt(SetMMOItemVariables.KEY_ITEM_ID);
			ISFSArray sFSArray = content.GetSFSArray(SetMMOItemVariables.KEY_VAR_LIST);
			MMORoom mMORoom = sfs.GetRoomById(@int) as MMORoom;
			List<string> list = new List<string>();
			if (mMORoom == null)
			{
				return;
			}
			IMMOItem mMOItem = mMORoom.GetMMOItem(int2);
			if (mMOItem != null)
			{
				for (int i = 0; i < sFSArray.Size(); i++)
				{
					IMMOItemVariable iMMOItemVariable = MMOItemVariable.FromSFSArray(sFSArray.GetSFSArray(i));
					mMOItem.SetVariable(iMMOItemVariable);
					list.Add(iMMOItemVariable.Name);
				}
				dictionary["changedVars"] = list;
				dictionary["mmoItem"] = mMOItem;
				dictionary["room"] = mMORoom;
				sfs.DispatchEvent(new SFSEvent(SFSEvent.MMOITEM_VARIABLES_UPDATE, dictionary));
			}
		}

		private void PopulateRoomList(ISFSArray roomList)
		{
			IRoomManager roomManager = sfs.RoomManager;
			for (int i = 0; i < roomList.Size(); i++)
			{
				ISFSArray sFSArray = roomList.GetSFSArray(i);
				Room room = SFSRoom.FromSFSArray(sFSArray);
				roomManager.ReplaceRoom(room);
			}
		}

		private void FnGenericMessage(IMessage msg)
		{
			ISFSObject content = msg.Content;
			switch (content.GetByte(GenericMessageRequest.KEY_MESSAGE_TYPE))
			{
			case 0:
				HandlePublicMessage(content);
				break;
			case 1:
				HandlePrivateMessage(content);
				break;
			case 5:
				HandleBuddyMessage(content);
				break;
			case 2:
				HandleModMessage(content);
				break;
			case 3:
				HandleAdminMessage(content);
				break;
			case 4:
				HandleObjectMessage(content);
				break;
			}
		}

		private void HandlePublicMessage(ISFSObject sfso)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = sfso.GetInt(GenericMessageRequest.KEY_ROOM_ID);
			Room roomById = sfs.RoomManager.GetRoomById(@int);
			if (roomById != null)
			{
				dictionary["room"] = roomById;
				dictionary["sender"] = sfs.UserManager.GetUserById(sfso.GetInt(GenericMessageRequest.KEY_USER_ID));
				dictionary["message"] = sfso.GetUtfString(GenericMessageRequest.KEY_MESSAGE);
				dictionary["data"] = sfso.GetSFSObject(GenericMessageRequest.KEY_XTRA_PARAMS);
				sfs.DispatchEvent(new SFSEvent(SFSEvent.PUBLIC_MESSAGE, dictionary));
			}
			else
			{
				log.Warn("Unexpected, PublicMessage target room doesn't exist. RoomId: " + @int);
			}
		}

		public void HandlePrivateMessage(ISFSObject sfso)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = sfso.GetInt(GenericMessageRequest.KEY_USER_ID);
			User user = sfs.UserManager.GetUserById(@int);
			if (user == null)
			{
				if (!sfso.ContainsKey(GenericMessageRequest.KEY_SENDER_DATA))
				{
					log.Warn("Unexpected. Private message has no Sender details!");
					return;
				}
				user = SFSUser.FromSFSArray(sfso.GetSFSArray(GenericMessageRequest.KEY_SENDER_DATA));
			}
			dictionary["sender"] = user;
			dictionary["message"] = sfso.GetUtfString(GenericMessageRequest.KEY_MESSAGE);
			dictionary["data"] = sfso.GetSFSObject(GenericMessageRequest.KEY_XTRA_PARAMS);
			sfs.DispatchEvent(new SFSEvent(SFSEvent.PRIVATE_MESSAGE, dictionary));
		}

		public void HandleBuddyMessage(ISFSObject sfso)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = sfso.GetInt(GenericMessageRequest.KEY_USER_ID);
			Buddy buddyById = sfs.BuddyManager.GetBuddyById(@int);
			dictionary["isItMe"] = (sfs.MySelf.Id == @int);
			dictionary["buddy"] = buddyById;
			dictionary["message"] = sfso.GetUtfString(GenericMessageRequest.KEY_MESSAGE);
			dictionary["data"] = sfso.GetSFSObject(GenericMessageRequest.KEY_XTRA_PARAMS);
			sfs.DispatchEvent(new SFSBuddyEvent(SFSBuddyEvent.BUDDY_MESSAGE, dictionary));
		}

		public void HandleModMessage(ISFSObject sfso)
		{
			HandleModMessage(sfso, SFSEvent.MODERATOR_MESSAGE);
		}

		public void HandleAdminMessage(ISFSObject sfso)
		{
			HandleModMessage(sfso, SFSEvent.ADMIN_MESSAGE);
		}

		private void HandleModMessage(ISFSObject sfso, string evt)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["sender"] = SFSUser.FromSFSArray(sfso.GetSFSArray(GenericMessageRequest.KEY_SENDER_DATA));
			dictionary["message"] = sfso.GetUtfString(GenericMessageRequest.KEY_MESSAGE);
			dictionary["data"] = sfso.GetSFSObject(GenericMessageRequest.KEY_XTRA_PARAMS);
			sfs.DispatchEvent(new SFSEvent(evt, dictionary));
		}

		public void HandleObjectMessage(ISFSObject sfso)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			int @int = sfso.GetInt(GenericMessageRequest.KEY_USER_ID);
			dictionary["sender"] = sfs.UserManager.GetUserById(@int);
			dictionary["message"] = sfso.GetSFSObject(GenericMessageRequest.KEY_XTRA_PARAMS);
			sfs.DispatchEvent(new SFSEvent(SFSEvent.OBJECT_MESSAGE, dictionary));
		}
	}
}
