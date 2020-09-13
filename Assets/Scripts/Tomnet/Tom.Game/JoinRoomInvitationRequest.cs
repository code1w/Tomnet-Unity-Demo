using Tom.Entities;
using Tom.Entities.Data;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests.Game
{
    public class JoinRoomInvitationRequest : BaseRequest
    {
        public static readonly string KEY_ROOM_ID = "r";

        public static readonly string KEY_EXPIRY_SECONDS = "es";

        public static readonly string KEY_INVITED_NAMES = "in";

        public static readonly string KEY_AS_SPECT = "as";

        public static readonly string KEY_OPTIONAL_PARAMS = "op";

        private Room targetRoom;

        private List<string> invitedUserNames;

        private int expirySeconds;

        private bool asSpectator;

        private ISFSObject parameters;

        public JoinRoomInvitationRequest(Room targetRoom, List<string> invitedUserNames, ISFSObject parameters, int expirySeconds, bool asSpectator)
            : base(RequestType.JoinRoomInvite)
        {
            Init(targetRoom, invitedUserNames, parameters, expirySeconds, asSpectator);
        }

        public JoinRoomInvitationRequest(Room targetRoom, List<string> invitedUserNames, ISFSObject parameters, int expirySeconds)
            : base(RequestType.JoinRoomInvite)
        {
            Init(targetRoom, invitedUserNames, parameters, expirySeconds, asSpectator: false);
        }

        public JoinRoomInvitationRequest(Room targetRoom, List<string> invitedUserNames, ISFSObject parameters)
            : base(RequestType.JoinRoomInvite)
        {
            Init(targetRoom, invitedUserNames, parameters, 30, asSpectator: false);
        }

        public JoinRoomInvitationRequest(Room targetRoom, List<string> invitedUserNames)
            : base(RequestType.JoinRoomInvite)
        {
            Init(targetRoom, invitedUserNames, null, 30, asSpectator: false);
        }

        private void Init(Room targetRoom, List<string> invitedUserNames, ISFSObject parameters, int expirySeconds, bool asSpectator)
        {
            this.targetRoom = targetRoom;
            this.invitedUserNames = invitedUserNames;
            this.expirySeconds = expirySeconds;
            this.asSpectator = asSpectator;
            ISFSObject iSFSObject2;
            if (parameters == null)
            {
                ISFSObject iSFSObject = new SFSObject();
                iSFSObject2 = iSFSObject;
            }
            else
            {
                iSFSObject2 = parameters;
            }
            this.parameters = iSFSObject2;
        }

        public override void Validate(TomOrange sfs)
        {
            List<string> list = new List<string>();
            if (targetRoom == null)
            {
                list.Add("Missing target room");
            }
            else if (invitedUserNames == null || invitedUserNames.Count < 1)
            {
                list.Add("No invitees provided");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("JoinRoomInvitationRequest request error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
            sfso.PutInt(KEY_ROOM_ID, targetRoom.Id);
            sfso.PutUtfStringArray(KEY_INVITED_NAMES, invitedUserNames.ToArray());
            sfso.PutSFSObject(KEY_OPTIONAL_PARAMS, parameters);
            sfso.PutInt(KEY_EXPIRY_SECONDS, expirySeconds);
            sfso.PutBool(KEY_AS_SPECT, asSpectator);
        }
    }
}
