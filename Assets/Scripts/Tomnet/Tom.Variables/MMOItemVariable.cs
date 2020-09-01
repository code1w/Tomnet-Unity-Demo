using Tom.Entities.Data;

namespace Tom.Entities.Variables
{
	public class MMOItemVariable : BaseVariable, IMMOItemVariable, Variable
	{
		public static IMMOItemVariable FromSFSArray(ISFSArray sfsa)
		{
			return new MMOItemVariable(sfsa.GetUtfString(0), sfsa.GetElementAt(2), sfsa.GetByte(1));
		}

		public MMOItemVariable(string name, object val, int type)
			: base(name, val, type)
		{
		}

		public MMOItemVariable(string name, object val)
			: base(name, val)
		{
		}

		public override string ToString()
		{
			return string.Concat("[MMOItemVar: ", name, ", type: ", type, ", value: ", val, "]");
		}
	}
}
