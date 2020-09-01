using Tom.Entities.Data;
using System.Collections.Generic;

namespace Tom.Entities
{
	public class MMORoom : SFSRoom
	{
		private Vec3D defaultAOI;

		private Vec3D lowerMapLimit;

		private Vec3D higherMapLimit;

		private Dictionary<int, IMMOItem> itemsById = new Dictionary<int, IMMOItem>();

		public Vec3D DefaultAOI
		{
			get
			{
				return defaultAOI;
			}
			set
			{
				defaultAOI = value;
			}
		}

		public Vec3D LowerMapLimit
		{
			get
			{
				return lowerMapLimit;
			}
			set
			{
				lowerMapLimit = value;
			}
		}

		public Vec3D HigherMapLimit
		{
			get
			{
				return higherMapLimit;
			}
			set
			{
				higherMapLimit = value;
			}
		}

		public MMORoom(int id, string name, string groupId)
			: base(id, name, groupId)
		{
		}

		public MMORoom(int id, string name)
			: base(id, name)
		{
		}

		public IMMOItem GetMMOItem(int id)
		{
			itemsById.TryGetValue(id, out IMMOItem value);
			return value;
		}

		public List<IMMOItem> GetMMOItems()
		{
			return new List<IMMOItem>(itemsById.Values);
		}

		public void AddMMOItem(IMMOItem item)
		{
			itemsById[item.Id] = item;
		}

		public void RemoveItem(int id)
		{
			itemsById.Remove(id);
		}
	}
}
