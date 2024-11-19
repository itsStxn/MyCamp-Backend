using Server.Models;

namespace Server.Interfaces;

public interface IUserService {
	Admin? AuthenticateAdmin(User user);
	bool UpdateUser(User user);
	bool DisableUser(int userID);
	void ElevateUser(int userID);
	void DemoteUser(int userID);
}
