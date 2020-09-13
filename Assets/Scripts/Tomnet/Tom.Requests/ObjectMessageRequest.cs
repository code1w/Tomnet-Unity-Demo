using Tom.Entities;
using Tom.Entities.Data;
using System.Collections.Generic;

namespace Tom.Requests
{
    public class ObjectMessageRequest : GenericMessageRequest
    {
        public ObjectMessageRequest(ISFSObject obj, Room targetRoom, ICollection<User> recipients)
        {
            type = 4;
            parameters = obj;
            room = targetRoom;
            recipient = recipients;
        }

        public ObjectMessageRequest(ISFSObject obj, Room targetRoom)
            : this(obj, targetRoom, null)
        {
        }

        public ObjectMessageRequest(ISFSObject obj)
            : this(obj, null, null)
        {
        }
    }
}
