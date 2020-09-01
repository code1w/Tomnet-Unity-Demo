using Tom.Entities.Data;
using Tom.Entities.Variables;
using System.Collections.Generic;

namespace Tom.Entities
{
	public interface IMMOItem
	{
		int Id
		{
			get;
		}

		Vec3D AOIEntryPoint
		{
			get;
			set;
		}

		List<IMMOItemVariable> GetVariables();

		IMMOItemVariable GetVariable(string name);

		void SetVariable(IMMOItemVariable variable);

		void SetVariables(List<IMMOItemVariable> variables);

		bool ContainsVariable(string name);
	}
}
