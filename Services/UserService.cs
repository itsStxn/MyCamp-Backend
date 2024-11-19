using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;
using Dapper;

namespace Server.Services;

public class UserService(IDbConnection db) : IUserService {
	private readonly IDbConnection Db = db;

/// <summary>
/// Authenticates an admin user by querying the database with the provided user details.
/// </summary>
/// <param name="user">The User object containing the details for authentication.</param>
/// <returns>The Admin object if the user is authenticated as an admin, otherwise null.</returns>
	public Admin? AuthenticateAdmin(User user) {
		string selectQuery = SelectAdminQuery();
		var admin = Db.QueryFirstOrDefault<Admin?>(selectQuery, user);
		return admin;
	}
/// <summary>
/// Updates a user in the database with the provided details.
/// The function first adjusts and formats the fields of the provided user.
/// It then checks if the user does not already exist and if the profile picture is valid.
/// If either of these conditions are not met, the function throws an InvalidOperationException.
/// The password is then hashed and the update query is executed.
/// </summary>
/// <param name="user">The User object containing the updated details.</param>
/// <returns>True if the user was successfully updated, otherwise false.</returns>
/// <exception cref="InvalidOperationException">Thrown when the user already exists or the profile picture is invalid.</exception>
	public bool UpdateUser(User user) {
		FormHelper.AdjustFields(user);
		FormHelper.RespectFormFormats(user);
		if (!AvailableIdentifiers(user)) {
			throw new InvalidOperationException("User already exists");
		}
		if (!ValidProfilePic(user.ProfilePicID)) {
			throw new InvalidOperationException("Invalid profile pic");
		}

		user.Active = true;
		user.Password = FormHelper.HashPassword(user.Password);
		string updateQuery = UserUpdateQuery();
		return Db.Execute(updateQuery, user) > 0;
	}
/// <summary>
/// Disables a user by setting the user's status in the database to inactive.
/// </summary>
/// <param name="userID">The ID of the user to disable.</param>
/// <returns>True if the user was successfully disabled, otherwise false.</returns>
	public bool DisableUser(int userID) {
		string updateQuery = DisableUserQuery();
		return Db.Execute(updateQuery, new { id = userID }) > 0;
	}
/// <summary>
/// Elevates a user to an admin role by inserting the user into the admin table.
/// </summary>
/// <param name="userID">The ID of the user to elevate.</param>
/// <exception cref="KeyNotFoundException">Thrown when the user does not exist.</exception>
	public void ElevateUser(int userID) {
		if (GetUser(userID) == null) {
			throw new KeyNotFoundException("User does not exist");
		}

		string insertAdminQuery = InsertAdminQuery();
		Db.Execute(insertAdminQuery, new { userID });
	}
/// <summary>
/// Demotes an admin user back to a normal user role by deleting the user from the admin table.
/// </summary>
/// <param name="userID">The ID of the user to demote.</param>
/// <exception cref="KeyNotFoundException">Thrown when the user is not an admin.</exception>
	public void DemoteUser(int userID) {
		if (GetAdmin(userID) == null) {
			throw new KeyNotFoundException("User is not an admin");
		}

		string deleteAdminQuery = DeleteAdminQuery();
		Db.Execute(deleteAdminQuery, new { userID });
	}

	#region Aid Functions
	
/// <summary>
/// Retrieves a user by ID from the database.
/// </summary>
/// <param name="userID">The ID of the user to retrieve.</param>
/// <returns>The user if found, otherwise null.</returns>
	private User? GetUser(int userID) {
		string selectQuery = SelectUserQuery();
		return Db.QueryFirstOrDefault<User?>(selectQuery, new { userID });
	}
/// <summary>
/// Retrieves an admin by ID from the database.
/// </summary>
/// <param name="userID">The ID of the admin to retrieve.</param>
/// <returns>The admin if found, otherwise null.</returns>
	private Admin? GetAdmin(int userID) {
		string selectQuery = SelectAdminQuery();
		return Db.QueryFirstOrDefault<Admin?>(selectQuery, new { id = userID });
	}
/// <summary>
/// Checks if the given user's username and email are available.
/// That is, there is no other user in the database with the same username or email.
/// </summary>
/// <param name="user">The user to check.</param>
/// <returns>True if the identifiers are available, false otherwise.</returns>
	private bool AvailableIdentifiers(User user) {
		string selectQuery = "SELECT * FROM users WHERE id != @id AND (username = @username OR email = @email);";
		var foundUser = Db.QueryFirstOrDefault<User?>(selectQuery, user);
		return foundUser == null;
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
	
	private static string DeleteAdminQuery() {
		return "DELETE FROM admins WHERE userID = @userID";
	}
	private static string SelectUserQuery() {
		return "SELECT * FROM users WHERE id = @userID AND active = 1";
	}
	private static string InsertAdminQuery() {
		return "INSERT INTO admins (userID) VALUES (@userID)";
	}
	private static string UserUpdateQuery() {
		return @$"
			UPDATE users 
			SET name = @name, surname = @surname, username = @username,
			email = @email, password = @password, profilePicID = @profilePicID
			WHERE id = @id
		";
	}
	private static string DisableUserQuery() {
		return "UPDATE users SET active = 0 WHERE id = @id";
	}
	private static string SelectAdminQuery() {
		return "SELECT * FROM admins WHERE userID = @id";
	}
	private static string SelectProfilePicQuery() {
		return "SELECT * FROM profilePics WHERE id = @profilePicID";
	}

	#endregion
}
