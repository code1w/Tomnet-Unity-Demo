using Tom.Entities.Data;
using System;

namespace Tom.Requests
{
    public class ModeratorMessageRequest : GenericMessageRequest
    {
        public ModeratorMessageRequest(string message, MessageRecipientMode recipientMode, ISFSObject parameters)
        {
            if (recipientMode == null)
            {
                throw new ArgumentException("RecipientMode cannot be null!");
            }
            type = 2;
            base.message = message;
            base.parameters = parameters;
            recipient = recipientMode.Target;
            sendMode = recipientMode.Mode;
        }

        public ModeratorMessageRequest(string message, MessageRecipientMode recipientMode)
            : this(message, recipientMode, null)
        {
        }
    }
}
