using Tom.Entities;
using Tom.Entities.Data;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
    public class ExtensionRequest : BaseRequest
    {
        public static readonly string KEY_CMD = "c";

        public static readonly string KEY_PARAMS = "p";

        public static readonly string KEY_ROOM = "r";

        private string extCmd;

        private ISFSObject parameters;

        private Room room;

        private bool useUDP;

        public bool UseUDP => useUDP;

        private void Init(string extCmd, ISFSObject parameters, Room room, bool useUDP)
        {
            targetController = 1;
            this.extCmd = extCmd;
            this.parameters = parameters;
            this.room = room;
            this.useUDP = useUDP;
            if (parameters == null)
            {
                parameters = new SFSObject();
            }
        }

        public ExtensionRequest(string extCmd, ISFSObject parameters, Room room, bool useUDP)
            : base(RequestType.CallExtension)
        {
            Init(extCmd, parameters, room, useUDP);
        }

        public ExtensionRequest(string extCmd, ISFSObject parameters, Room room)
            : base(RequestType.CallExtension)
        {
            Init(extCmd, parameters, room, useUDP: false);
        }

        public ExtensionRequest(string extCmd, ISFSObject parameters)
            : base(RequestType.CallExtension)
        {
            Init(extCmd, parameters, null, useUDP: false);
        }

        public override void Validate(TomOrange sfs)
        {
            List<string> list = new List<string>();
            if (extCmd == null || extCmd.Length == 0)
            {
                list.Add("Missing extension command");
            }
            if (parameters == null)
            {
                list.Add("Missing extension parameters");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("ExtensionCall request error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
            sfso.PutUtfString(KEY_CMD, extCmd);
            sfso.PutInt(KEY_ROOM, (room == null) ? (-1) : room.Id);
            sfso.PutSFSObject(KEY_PARAMS, parameters);
        }
    }
}
