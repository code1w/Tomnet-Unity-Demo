using System.Collections.Generic;

namespace Tom.Entities.Managers
{
	public interface IUserManager
	{
		int UserCount
		{
			get;
		}

		TomOrange SmartFoxClient
		{
			get;
		}

		bool ContainsUserName(string userName);

		bool ContainsUserId(int userId);

		bool ContainsUser(User user);

		User GetUserByName(string userName);

		User GetUserById(int userId);

		void AddUser(User user);

		void RemoveUser(User user);

		void RemoveUserById(int id);

		List<User> GetUserList();

		void ReplaceAll(List<User> newUserList);
	}
}
