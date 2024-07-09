using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;
using Dapper;

namespace Server.Services;

public class AuthUserService(
	IDbConnection db, 
	ISecretKeyService secretService, 
	IUserService userService,
	IEmailService emailService
) : IAuthUserService {
	private readonly IDbConnection Db = db;
	private readonly ISecretKeyService Secret = secretService;
	private readonly IUserService User = userService;
	private readonly IEmailService EmailService = emailService;
	private readonly FormHelper Form = new();
	
	private User? SearchUser(AuthUser user) {
		string email = user.Email;
		string username = user.Username;
		string selectQuery = SelectUserQuery();
		return Db.QueryFirstOrDefault<User?>(selectQuery, new { username, email });
	}
	public AuthTicket? AuthenticateUser(AuthUser user) {
		var jwtH = new JwtHandler(Secret.GetSecretKey());
		Form.RespectPasswordFormat(user.Password);
		var foundUser = SearchUser(user) ??
		throw new KeyNotFoundException("User not found");

		if (!foundUser.Active) throw 
		new UnauthorizedAccessException("User is not active");
    
		var form = new FormHelper();
		if (!form.VerifyPassword(foundUser.Password, user.Password)) return null;
		foundUser.Password = string.Empty;

		foundUser.Role = GetUserRole(foundUser);
		var token = jwtH.GenerateJwt(foundUser.Id, foundUser.Role);
		return new AuthTicket { JWT = token, User = foundUser };
	}
	public bool RegisterUser(User user) {
		string secretKey = Secret.GetSecretKey();
		Form.AdjustFields(user);
		Form.RespectFormFormats(user);
		User? foundUser = SearchUser(new() {
			Username = user.Username,
			Password = user.Password,
			Email = user.Email
		});
		
		if (foundUser == null) return InsertUserAndSendMail(user, secretKey);
		if (foundUser.Active) throw new UnauthorizedAccessException("User already exists");
		return ResumeUserAndSendMail(foundUser.Id, user);
	}
	public string? MailCredentialsSetter(MailCredentials cred) {
		string secretKey = Secret.GetSecretKey();
		AuthUser user = new() {
			Username = cred.IdentityInput,
			Email = cred.IdentityInput,
			Password = string.Empty
		};

		User? foundUser = SearchUser(user) ??
		throw new KeyNotFoundException("User not found");

		var dtm = DateTime.UtcNow.AddMinutes(15);
		return 	ComposeMail(secretKey, foundUser.Id, foundUser.Email, dtm, "password reset")
						? foundUser.Email : null;
	}
	public bool ChangePassword(string token, int userID, string newPassword) {
		Form.RespectPasswordFormat(newPassword);
		userID = int.Parse(CheckToken(token));
		newPassword = new FormHelper().HashPassword(newPassword);
		string updateQuery = UpdatePasswordQuery();

		return UpdateUserAndDeleteToken(
			new { newPassword, userID, token },
			updateQuery, 
			userID, 
			token
		);
	}
	public bool ActivateAccount(string token) {
		int userID = int.Parse(CheckToken(token));
		string updateQuery = ActivateUserQuery();
		return UpdateUserAndDeleteToken(
			new { userID },
			updateQuery, 
			userID, 
			token
		);
	}

	#region Aid Functions

	private bool ResumeUserAndSendMail(int oldUserID, User user) {
		if (!ValidProfilePic(user.ProfilePicID)) {
			throw new InvalidOperationException("Invalid user profile pic");
		}
		user.Password = Form.HashPassword(user.Password);
		string secretKey = Secret.GetSecretKey();
		user.Id = oldUserID;
		
		string updateQuery = UpdateUserProfileQuery();
		if (Db.Execute(updateQuery, user) == 0) {
			throw new InvalidOperationException("Invalid user insert query");
		}
		return ComposeMail(
			secretKey, 
			user.Id, 
			user.Email, 
			DateTime.UtcNow.AddHours(1), 
			"activate account");
	}
	private string GetUserRole(User user) {
		var admin = User.AuthenticateAdmin(user);
		return admin != null ? "admin" : "user";
	}
	public bool UpdateUserAndDeleteToken(object queryParams, string updateQuery, int userID, string token) {
		if (Db.State == ConnectionState.Closed) Db.Open();
		using var trans = Db.BeginTransaction();
		
		try {
			int res = Db.Execute(updateQuery, queryParams, trans);
			if (res == 0) throw new UnauthorizedAccessException();
			if (!DeleteToken(token, userID, trans)) {
				trans.Rollback();
				return false;
			}
			trans.Commit();
			return true;
		}
		catch (UnauthorizedAccessException) {
			trans.Rollback();
			throw new UnauthorizedAccessException
			("Invalid or expired token");
		}
		catch (Exception e) {
			trans.Rollback();
			throw new Exception(e.Message);
		}
		finally {
			if (Db.State == ConnectionState.Open) Db.Close();
		}
	}
	private bool InsertUserAndSendMail(User user, string secretKey) {
		if (!ValidProfilePic(user.ProfilePicID)) {
			throw new InvalidOperationException("Invalid user profile pic");
		}
		var dtm = DateTime.UtcNow;
		user.Password = Form.HashPassword(user.Password);
		string insertQuery = UserRegistrationQuery(dtm);

		if (Db.State == ConnectionState.Closed) Db.Open();
		using var trans = Db.BeginTransaction();
		try {
			string? res = Db.QueryFirstOrDefault<string?>
			(insertQuery, user, trans) ?? throw 
			new InvalidOperationException();
			
			bool sent = ComposeMail(
				secretKey, 
				int.Parse(res), 
				user.Email, 
				dtm.AddHours(1), 
				"activate account",
				trans);

			if (!sent) {
				trans.Rollback();
				return false;
			}
			trans.Commit(); 
			return true;
		}
		catch (InvalidOperationException) {
			trans.Rollback();
			throw new InvalidOperationException
			("Invalid user insert query");
		}
		catch (Exception e) {
			trans.Rollback();
			throw new Exception(e.Message);
		}
		finally {
			if (Db.State == ConnectionState.Open) Db.Close();
		}
	}
	private string CheckToken(string token) {
		var jwtH = new JwtHandler(Secret.GetSecretKey());
		jwtH.DecodeJwt(token);

		var expiry = jwtH.GetDecodedExpiry();
		var userIdClaim = jwtH.GetDecodedSub();
		if (expiry == null || userIdClaim == null || expiry < DateTime.UtcNow) {
			throw new UnauthorizedAccessException("Invalid or expired token");
		}

		return userIdClaim;
	}
	private bool AddToken(string token, int userID, DateTime expiry, IDbTransaction? transaction = null) {
		string insertQuery = InsertTokenQuery();
		int res = Db.Execute(insertQuery, new { token, userID, expiry }, transaction);
		return res > 0;
	}
	private bool DeleteToken(string token, int userID, IDbTransaction? transaction = null) {
		string deleteQuery = DeleteTokenQuery();
		int res = Db.Execute(deleteQuery, new { token, userID }, transaction);
		return res > 0;
	}
	private bool SendMail(string email, string token, string action) {
		if (action == "activate account") {
			EmailService.SendActivationMail(email, token);
		} else {
			EmailService.SendResetPasswordMail(email, token);
		}
		return true;
	}
	private bool ComposeMail(string secretKey, int userID, string email, DateTime exp, string action, IDbTransaction? transaction = null) {
		var jwtH = new JwtHandler(secretKey);
		string? token = SearchToken(userID);
		if (token != null) return SendMail(email, token, action);

		token = jwtH.GenerateJwt(userID, "user", exp);
		return AddToken(token, userID, exp, transaction) && SendMail(email, token, action);
	}
	private string? SearchToken(int userID) {
		string selectQuery = SelectTokenQuery();
		var ticket = Db.QueryFirstOrDefault<TokenTicket?>(selectQuery, new { userID });
		if (ticket == null || ticket.Expiry < DateTime.UtcNow) return null;
		return ticket.Token;
	}
	private bool ValidProfilePic(int profilePicID) {
		string selectQuery = SelectProfilePicQuery();
		var foundProfilePic = Db.QueryFirstOrDefault<ProfilePic?>(selectQuery, new { profilePicID });
		return foundProfilePic != null;
	}

	#endregion

	#region Queries
		
	private static string SelectProfilePicQuery() {
		return "SELECT * FROM profilePics WHERE id = @profilePicID";
	}
	private static string SelectUserQuery() {
		return "SELECT * FROM users WHERE username = @username OR email = @email";
	}
	private static string ActivateUserQuery() {
		return "UPDATE users SET active = 1 WHERE id = @userID AND active = 0;";
	}
	private static string UpdateUserProfileQuery() {
		return @$"
			UPDATE users 
			SET name = @name, surname = @surname, username = @username,
					email = @email, password = @password, profilePicID = @profilePicID
			WHERE id = @id
		";
	}
	private static string UserRegistrationQuery(DateTime dtm) {
		var timestamp = dtm.ToString("yyyy-MM-dd HH:mm:ss");
		return @$"
			INSERT INTO users (name, surname, username, email, password, createdAt, profilePicID) 
			VALUES (@name, @surname, @username, @email, @password, '{timestamp}', @profilePicID);
			SELECT LAST_INSERT_ID();
		";
	}
	private static string UpdatePasswordQuery() {
		var dtm = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
		return @$"
			UPDATE users SET password = @newPassword 
			WHERE id = @userID
			AND
			id IN (
				SELECT userID FROM tokens 
				WHERE token = @token AND expiry > '{dtm}'
			);
		";
	}
	private static string InsertTokenQuery() {
		return "INSERT INTO tokens (token, userID, expiry) VALUES (@token, @userID, @expiry);";
	}
	private static string DeleteTokenQuery() {
		return "DELETE FROM tokens WHERE token = @token AND userID = @userID;";
	}
	private static string SelectTokenQuery() {
		return "SELECT * FROM tokens WHERE userID = @userID LIMIT 1;";
	}

	#endregion
}
