using Server.Models;

namespace Server.Interfaces;

public interface IAuthUserService {
	AuthTicket? AuthenticateUser(AuthUser user);
	bool RegisterUser(User user);
	string? MailCredentialsSetter(MailCredentials cred);
	bool ChangePassword(string token, int userID, string newPassword);
	bool ActivateAccount(string token);
}
