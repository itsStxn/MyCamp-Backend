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

/// <summary>
/// Searches for a user in the database by their username or email.
/// </summary>
/// <param name="user">The AuthUser object containing the username and email to search for.</param>
/// <returns>The User object if found, otherwise null.</returns>
	private User? SearchUser(AuthUser user) {
		string email = user.Email;
		string username = user.Username;
		string selectQuery = SelectUserQuery();
		return Db.QueryFirstOrDefault<User?>(selectQuery, new { username, email });
	}
/// <summary>
/// Authenticates a user by searching for them in the database and verifying the password.
/// If the user is found and the password is correct, returns an AuthTicket containing the JWT and the User.
/// If the user is not found, or the password is incorrect, returns null.
/// If the user is not active, throws an UnauthorizedAccessException.
/// </summary>
/// <param name="user">The AuthUser object containing the username and password to authenticate.</param>
/// <returns>An AuthTicket if the user is authenticated, otherwise null.</returns>
	public AuthTicket? AuthenticateUser(AuthUser user) {
		var jwtH = new JwtHandler(Secret.GetSecretKey());
		FormHelper.RespectPasswordFormat(user.Password);
		var foundUser = SearchUser(user) ??
		throw new KeyNotFoundException("User not found");

		if (!foundUser.Active) throw 
		new UnauthorizedAccessException("User is not active");
	
		if (!FormHelper.VerifyPassword(foundUser.Password, user.Password)) return null;
		foundUser.Password = string.Empty;

		foundUser.Role = GetUserRole(foundUser);
		var token = jwtH.GenerateJwt(foundUser.Id, foundUser.Role);
		return new AuthTicket { JWT = token, User = foundUser };
	}
/// <summary>
/// Registers a new user in the system. 
/// If a user with the same username or email does not already exist, inserts the user and sends a confirmation email.
/// If the user exists but is not active, resumes the user and sends a confirmation email.
/// Throws an exception if the user already exists and is active.
/// </summary>
/// <param name="user">The User object containing the registration details.</param>
/// <returns>True if the user was successfully registered or resumed, otherwise false.</returns>
	public bool RegisterUser(User user) {
		string secretKey = Secret.GetSecretKey();
		FormHelper.AdjustFields(user);
		FormHelper.RespectFormFormats(user);
		User? foundUser = SearchUser(new() {
			Username = user.Username,
			Password = user.Password,
			Email = user.Email
		});
		
		if (foundUser == null) return InsertUserAndSendMail(user, secretKey);
		if (foundUser.Active) throw new UnauthorizedAccessException("User already exists");
		return ResumeUserAndSendMail(foundUser.Id, user);
	}
/// <summary>
/// Sends a password reset link to the user with the given email or username.
/// If a user with the given email or username does not exist, throws a KeyNotFoundException.
/// </summary>
/// <param name="cred">The MailCredentials object containing the user email or username.</param>
/// <returns>The email address if the link was sent, otherwise null.</returns>
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
		return ComposeMail(secretKey, foundUser.Id, foundUser.Email, dtm, "password reset")
					? foundUser.Email : null;
	}
/// <summary>
/// Changes the user's password given a valid reset password token.
/// The user is identified by the given user ID.
/// The new password is hashed and then updated in the database.
/// The token is then deleted to prevent further use.
/// </summary>
/// <param name="token">The reset password token.</param>
/// <param name="userID">The user ID.</param>
/// <param name="newPassword">The new password.</param>
/// <returns>True if the password was changed successfully, otherwise false.</returns>
	public bool ChangePassword(string token, int userID, string newPassword) {
		FormHelper.RespectPasswordFormat(newPassword);
		userID = int.Parse(CheckToken(token));
		newPassword = FormHelper.HashPassword(newPassword);
		string updateQuery = UpdatePasswordQuery();

		return UpdateUserAndDeleteToken(
			new { newPassword, userID, token },
			updateQuery, 
			userID, 
			token
		);
	}
/// <summary>
/// Activates a user given a valid activation token.
/// The user is identified by the given user ID.
/// The user is then activated in the database.
/// The token is then deleted to prevent further use.
/// </summary>
/// <param name="token">The activation token.</param>
/// <returns>True if the user was activated successfully, otherwise false.</returns>
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

/// <summary>
/// Resumes an existing user and sends an activation email.
/// The function updates the user's profile with the provided details,
/// hashes the password, and sets the old user ID.
/// If the profile picture is invalid, throws an InvalidOperationException.
/// If the user update fails, throws an InvalidOperationException.
/// </summary>
/// <param name="oldUserID">The ID of the existing user to resume.</param>
/// <param name="user">The User object containing the updated details.</param>
/// <returns>True if the activation email was sent successfully, otherwise false.</returns>
/// <exception cref="InvalidOperationException">Thrown when the profile picture is invalid or user update fails.</exception>
	private bool ResumeUserAndSendMail(int oldUserID, User user) {
		if (!ValidProfilePic(user.ProfilePicID)) {
			throw new InvalidOperationException("Invalid user profile pic");
		}
		user.Password = FormHelper.HashPassword(user.Password);
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
/// <summary>
/// Returns the role of the given user, either "admin" or "user".
/// The function first checks if the given user is an admin using the AuthenticateAdmin function.
/// </summary>
/// <param name="user">The user object to get the role from.</param>
/// <returns>The role of the given user.</returns>
	private string GetUserRole(User user) {
		var admin = User.AuthenticateAdmin(user);
		return admin != null ? "admin" : "user";
	}
/// <summary>
/// Updates a user and deletes the given token.
/// The function first executes the given update query and checks if the result is 0.
/// If the result is 0, throws an UnauthorizedAccessException.
/// Then, it calls the DeleteToken function to delete the given token.
/// If the DeleteToken function returns false, it rolls back the transaction and returns false.
/// If the DeleteToken function returns true, it commits the transaction and returns true.
/// </summary>
/// <param name="queryParams">The query parameters to pass to the update query.</param>
/// <param name="updateQuery">The query to execute to update the user.</param>
/// <param name="userID">The ID of the user to update.</param>
/// <param name="token">The token to delete.</param>
/// <returns>True if the user was updated successfully, otherwise false.</returns>
/// <exception cref="UnauthorizedAccessException">Thrown when the token is invalid or expired.</exception>
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
/// <summary>
/// Inserts a new user into the database and sends an activation email.
/// The function first hashes the user's password and inserts the user into the database.
/// If the user is inserted successfully, it sends an activation email to the user.
/// If the email is sent successfully, it commits the transaction and returns true.
/// If the email is not sent successfully, it rolls back the transaction and returns false.
/// If an exception is thrown during the transaction, it rolls back the transaction and throws the exception.
/// </summary>
/// <param name="user">The User object containing the registration details.</param>
/// <param name="secretKey">The secret key to use to generate the JWT token.</param>
/// <returns>True if the user was inserted and the email was sent successfully, otherwise false.</returns>
/// <exception cref="InvalidOperationException">Thrown when the user insert query is invalid.</exception>
	private bool InsertUserAndSendMail(User user, string secretKey) {
		if (!ValidProfilePic(user.ProfilePicID)) {
			throw new InvalidOperationException("Invalid user profile pic");
		}
		var dtm = DateTime.UtcNow;
		user.Password = FormHelper.HashPassword(user.Password);
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
/// <summary>
/// Checks the validity of a JWT token and returns the user ID claim if the token is valid.
/// The function decodes the token, checks the expiration date and time, and verifies
/// that the user ID claim is present. If the token is invalid or expired, it throws
/// an UnauthorizedAccessException.
/// </summary>
/// <param name="token">The JWT token to check.</param>
/// <returns>The user ID claim if the token is valid, otherwise throws an exception.</returns>
/// <exception cref="UnauthorizedAccessException">Thrown when the token is invalid or expired.</exception>
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
/// <summary>
/// Adds a token to the tokens table.
/// The function executes the InsertTokenQuery query with the given parameters
/// and returns true if the query is successful, otherwise false.
/// </summary>
/// <param name="token">The token to add.</param>
/// <param name="userID">The user ID associated with the token.</param>
/// <param name="expiry">The expiry date and time of the token.</param>
/// <param name="transaction">The transaction to execute the query in. If null, a new transaction is started.</param>
/// <returns>True if the query is successful, otherwise false.</returns>
	private bool AddToken(string token, int userID, DateTime expiry, IDbTransaction? transaction = null) {
		string insertQuery = InsertTokenQuery();
		int res = Db.Execute(insertQuery, new { token, userID, expiry }, transaction);
		return res > 0;
	}
/// <summary>
/// Deletes a token from the tokens table.
/// The function executes the DeleteTokenQuery query with the given parameters
/// and returns true if the query is successful, otherwise false.
/// </summary>
/// <param name="token">The token to delete.</param>
/// <param name="userID">The user ID associated with the token.</param>
/// <param name="transaction">The transaction to execute the query in. If null, a new transaction is started.</param>
/// <returns>True if the query is successful, otherwise false.</returns>
	private bool DeleteToken(string token, int userID, IDbTransaction? transaction = null) {
		string deleteQuery = DeleteTokenQuery();
		int res = Db.Execute(deleteQuery, new { token, userID }, transaction);
		return res > 0;
	}
/// <summary>
/// Sends an activation or password reset email to the given email address.
/// </summary>
/// <param name="email">The email address to send the email to.</param>
/// <param name="token">The token to include in the email.</param>
/// <param name="action">The action to perform, either "activate account" or "password reset".</param>
/// <returns>True if the email was sent successfully, otherwise false.</returns>
	private bool SendMail(string email, string token, string action) {
		if (action == "activate account") {
			EmailService.SendActivationMail(email, token);
		} else {
			EmailService.SendResetPasswordMail(email, token);
		}
		return true;
	}
/// <summary>
/// Composes an email to send to the given email address.
/// The function first searches for an existing token for the given user ID.
/// If a token is found, it sends the email with the existing token.
/// If no token is found, it generates a new token, adds it to the database,
/// and sends the email with the new token.
/// The function returns true if the email was sent successfully, otherwise false.
/// </summary>
/// <param name="secretKey">The secret key to use to generate the JWT token.</param>
/// <param name="userID">The user ID to associate with the token.</param>
/// <param name="email">The email address to send the email to.</param>
/// <param name="exp">The expiry date and time of the token.</param>
/// <param name="action">The action to perform, either "activate account" or "password reset".</param>
/// <param name="transaction">The transaction to execute the query in. If null, a new transaction is started.</param>
/// <returns>True if the email was sent successfully, otherwise false.</returns>
	private bool ComposeMail(string secretKey, int userID, string email, DateTime exp, string action, IDbTransaction? transaction = null) {
		var jwtH = new JwtHandler(secretKey);
		string? token = SearchToken(userID);
		if (token != null) return SendMail(email, token, action);

		token = jwtH.GenerateJwt(userID, "user", exp);
		return AddToken(token, userID, exp, transaction) && SendMail(email, token, action);
	}
/// <summary>
/// Searches for a token associated with the given user ID.
/// If a token is found, it is returned if the token is still valid.
/// If the token is expired, or no token is found, the function returns null.
/// </summary>
/// <param name="userID">The user ID to search for.</param>
/// <returns>The token if found and valid, otherwise null.</returns>
	private string? SearchToken(int userID) {
		string selectQuery = SelectTokenQuery();
		var ticket = Db.QueryFirstOrDefault<TokenTicket?>(selectQuery, new { userID });
		if (ticket == null || ticket.Expiry < DateTime.UtcNow) return null;
		return ticket.Token;
	}
/// <summary>
/// Checks if a profile picture with the given ID exists in the database.
/// </summary>
/// <param name="profilePicID">The ID of the profile picture to check.</param>
/// <returns>True if the profile picture exists, otherwise false.</returns>
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
