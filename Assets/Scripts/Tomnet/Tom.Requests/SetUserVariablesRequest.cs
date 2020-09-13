using Tom.Entities.Data;
using Tom.Entities.Variables;
using Tom.Exceptions;
using System.Collections.Generic;

namespace Tom.Requests
{
    public class SetUserVariablesRequest : BaseRequest
    {
        public static readonly string KEY_USER = "u";

        public static readonly string KEY_VAR_LIST = "vl";

        private ICollection<UserVariable> userVariables;

        public SetUserVariablesRequest(ICollection<UserVariable> userVariables)
            : base(RequestType.SetUserVariables)
        {
            this.userVariables = userVariables;
        }

        public override void Validate(TomOrange sfs)
        {
            List<string> list = new List<string>();
            if (userVariables == null || userVariables.Count == 0)
            {
                list.Add("No variables were specified");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("SetUserVariables request error", list);
            }
        }

        public override void Execute(TomOrange sfs)
        {
            ISFSArray iSFSArray = SFSArray.NewInstance();
            foreach (UserVariable userVariable in userVariables)
            {
                iSFSArray.AddSFSArray(userVariable.ToSFSArray());
            }
            sfso.PutSFSArray(KEY_VAR_LIST, iSFSArray);
        }
    }
}
