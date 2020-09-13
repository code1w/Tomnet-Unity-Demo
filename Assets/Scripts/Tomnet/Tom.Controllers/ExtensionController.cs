using Tom.Bitswarm;
using Tom.Core;
using Tom.Entities.Data;
using System.Collections.Generic;

namespace Tom.Controllers
{
    public class ExtensionController : BaseController
    {
        public static readonly string KEY_CMD = "c";

        public static readonly string KEY_PARAMS = "p";

        public static readonly string KEY_ROOM = "r";

        public ExtensionController(ISocketClient socketClient)
            : base(socketClient)
        {
        }

        public override void HandleMessage(IMessage message)
        {
            if (sfs.Debug)
            {
                log.Info(message.ToString());
            }
            ISFSObject content = message.Content;
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["cmd"] = content.GetUtfString(KEY_CMD);
            dictionary["params"] = content.GetSFSObject(KEY_PARAMS);
            if (content.ContainsKey(KEY_ROOM))
            {
                int @int = content.GetInt(KEY_ROOM);
                dictionary["sourceRoom"] = @int;
                dictionary["room"] = sfs.GetRoomById(@int);
            }
            if (message.IsUDP)
            {
                dictionary["packetId"] = message.PacketId;
            }
            sfs.DispatchEvent(new SFSEvent(SFSEvent.EXTENSION_RESPONSE, dictionary));
        }
    }
}
