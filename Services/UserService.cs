using Server.Interfaces;
using Server.Models;
using Server.Utils;
using System.Data;
using Dapper;

namespace Server.Services;

public class UserService(IDbConnection db) : IUserService {
	private readonly IDbConnection Db = db;
	private readonly FormHelper Form = new();
	
	public Admin? AuthenticateAdmin(User user) {
		string selectQuery = SelectAdminQuery();
		var admin = Db.QueryFirstOrDefault<Admin?>(selectQuery, user);
		return admin;
	}
	public bool UpdateUser(User user) {
		Form.AdjustFields(user);
		Form.RespectFormFormats(user);
		if (!AvailableIdentifiers(user)) {
			throw new InvalidOperationException("User already exists");
		}
		if (!ValidProfilePic(user.ProfilePicID)) {
			throw new InvalidOperationException("Invalid profile pic");
		}

		user.Active = true;
		user.Password = Form.HashPassword(user.Password);
		string updateQuery = UserUpdateQuery();
		return Db.Execute(updateQuery, user) > 0;
	}
	public bool DisableUser(int userID) {
		string updateQuery = DisableUserQuery();
		return Db.Execute(updateQuery, new { id = userID }) > 0;
	}

	#region Aid Functions
		
		private bool AvailableIdentifiers(User user) {
			string selectQuery = "SELECT * FROM users WHERE id != @id AND (username = @username OR email = @email);";
			var foundUser = Db.QueryFirstOrDefault<User?>(selectQuery, user);
			return foundUser == null;
		}
		private bool ValidProfilePic(int profilePicID) {
			string selectQuery = SelectProfilePicQuery();
			var foundProfilePic = Db.QueryFirstOrDefault<ProfilePic?>(selectQuery, new { profilePicID });
			return foundProfilePic != null;
		}

	#endregion

	#region Queries
		
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
