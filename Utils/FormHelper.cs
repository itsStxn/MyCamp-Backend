using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Server.Models;
using System.Text;

namespace Server.Utils;

public static class FormHelper {
	private static readonly PasswordHasher<object> Hasher = new();
	private static readonly Dictionary<string, string> StateToCodeMap = new(StringComparer.OrdinalIgnoreCase) {
		{ "alabama", "AL" },
		{ "alaska", "AK" },
		{ "arizona", "AZ" },
		{ "arkansas", "AR" },
		{ "california", "CA" },
		{ "colorado", "CO" },
		{ "connecticut", "CT" },
		{ "delaware", "DE" },
		{ "florida", "FL" },
		{ "georgia", "GA" },
		{ "hawaii", "HI" },
		{ "idaho", "ID" },
		{ "illinois", "IL" },
		{ "indiana", "IN" },
		{ "iowa", "IA" },
		{ "kansas", "KS" },
		{ "kentucky", "KY" },
		{ "louisiana", "LA" },
		{ "maine", "ME" },
		{ "maryland", "MD" },
		{ "massachusetts", "MA" },
		{ "michigan", "MI" },
		{ "minnesota", "MN" },
		{ "mississippi", "MS" },
		{ "missouri", "MO" },
		{ "montana", "MT" },
		{ "nebraska", "NE" },
		{ "nevada", "NV" },
		{ "new hampshire", "NH" },
		{ "new jersey", "NJ" },
		{ "new mexico", "NM" },
		{ "new york", "NY" },
		{ "north carolina", "NC" },
		{ "north dakota", "ND" },
		{ "ohio", "OH" },
		{ "oklahoma", "OK" },
		{ "oregon", "OR" },
		{ "pennsylvania", "PA" },
		{ "rhode island", "RI" },
		{ "south carolina", "SC" },
		{ "south dakota", "SD" },
		{ "tennessee", "TN" },
		{ "texas", "TX" },
		{ "utah", "UT" },
		{ "vermont", "VT" },
		{ "virginia", "VA" },
		{ "washington", "WA" },
		{ "west virginia", "WV" },
		{ "wisconsin", "WI" },
		{ "wyoming", "WY" }
	};

	/// <summary>
	/// Given a state name, returns the associated state code.
	/// Case-insensitive.
	/// </summary>
	/// <param name="stateName">The name of a state</param>
	/// <returns>The state code, or <c>null</c> if not found</returns>
	public static string? GetStateCode(string stateName){
		StateToCodeMap.TryGetValue(stateName, out string? stateCode);
		return stateCode;
	}	
/// <summary>
/// Hashes a password using the built-in ASP.NET Core password hasher.
/// </summary>
/// <param name="password">The password to hash</param>
/// <returns>The hashed password</returns>
	public static string HashPassword(string password) {
		return Hasher.HashPassword(new {}, password);
	}
/// <summary>
/// Verifies a given password against a hashed password
/// </summary>
/// <param name="hashedPassword">The hashed password to verify against</param>
/// <param name="password">The password to verify</param>
/// <returns><c>true</c> if the password matches the hashed password, <c>false</c> otherwise</returns>
	public static bool VerifyPassword(string hashedPassword, string password) {
		var result = Hasher.VerifyHashedPassword(new {}, hashedPassword, password);
		return result == PasswordVerificationResult.Success;
	}
/// <summary>
/// Checks if a name and surname are in a valid format.
/// A valid format is a string consisting only of letters.
/// </summary>
/// <param name="name">The name to check</param>
/// <param name="surname">The surname to check</param>
/// <returns><c>true</c> if the name and surname are valid, <c>false</c> otherwise</returns>
	private static bool CheckNameFormat(string name, string surname) {
		string pattern = @"^[a-zA-Z]+$";
		return Regex.IsMatch(name, pattern) 
		&& Regex.IsMatch(surname, pattern);
	}
/// <summary>
/// Checks if a username is in a valid format.
/// A valid format is a string consisting only of letters (a-z or A-Z), numbers (0-9), and underscores (_).
/// </summary>
/// <param name="username">The username to check</param>
/// <returns><c>true</c> if the username is valid, <c>false</c> otherwise</returns>
	private static bool CheckUsernameFormat(string username) {
		string pattern = @"^[a-zA-Z0-9_]+$";
		return Regex.IsMatch(username, pattern);
	}
/// <summary>
/// Checks if an email is in a valid format.
/// A valid format is a string with the following properties:
///<br/> - Contains at least one '@' character
///<br/> - Contains at least one '.' character after the '@'
///<br/> - Contains only letters (a-z or A-Z), numbers (0-9), periods (.), hyphens (-), underscores (_), and percent signs (%)
/// </summary>
/// <param name="email">The email to check</param>
/// <returns><c>true</c> if the email is valid, <c>false</c> otherwise</returns>
	private static bool CheckEmailFormat(string email) {
		string pattern = @"^([a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})$";
		return Regex.IsMatch(email, pattern);
	}
/// <summary>
/// Checks if a password is in a valid format.
/// A valid format is a string that meets all of the following criteria:
/// <br/> - Contains at least one number (0-9)
/// <br/> - Contains at least one special character (!, @, #, *, ?)
/// <br/> - Contains at least one uppercase letter (A-Z)
/// <br/> - Contains at least one lowercase letter (a-z)
/// <br/> - Contains at least 8 characters
/// <br/> - Contains only allowed characters (letters, numbers, special characters, and the following: _, ., -, _, %)
/// </summary>
/// <param name="password">The password to check</param>
/// <returns><c>true</c> if the password is valid, <c>false</c> otherwise</returns>
	private static bool CheckPasswordFormat(string password) {
		string pattern = @"^(?=.*[0-9])(?=.*[!@#*?])(?=.*[A-Z])(?=.*[a-z]).*[^\s'"";$%<>|`^~\\]+$";
		return Regex.IsMatch(password, pattern) && password.Length >= 8;
	}
/// <summary>
/// Validates the format of the user's name, username, email, and password.
/// Throws an exception if any format is invalid.
/// </summary>
/// <param name="user">The user object containing name, surname, username, email, and password to validate.</param>
	public static void RespectFormFormats(User user) {
		RespactNameFormat(user.Name, user.Surname);
		RespectUsernameFormat(user.Username);
		RespectEmailFormat(user.Email);
		RespectPasswordFormat(user.Password);
	}
/// <summary>
/// Validates the format of the given name and surname.
/// Throws an exception if the format is invalid.
/// </summary>
/// <param name="name">The name to validate.</param>
/// <param name="surname">The surname to validate.</param>
/// <exception cref="ArgumentException">Thrown when the name or surname does not respect the format.</exception>
	private static void RespactNameFormat(string name, string surname) {
		if (!CheckNameFormat(name, surname)) {
			throw new ArgumentException("Name or surname does not respect format");
		}
	}
/// <summary>
/// Validates the format of the given username.
/// Throws an exception if the format is invalid.
/// </summary>
/// <param name="username">The username to validate.</param>
/// <exception cref="ArgumentException">Thrown when the username does not respect the format.</exception>
	private static void RespectUsernameFormat(string username) {
		if (!CheckUsernameFormat(username)) {
			throw new ArgumentException("Username does not respect format");
		}
	}
/// <summary>
/// Validates the format of the given email.
/// Throws an exception if the format is invalid.
/// </summary>
/// <param name="email">The email to validate.</param>
/// <exception cref="ArgumentException">Thrown when the email does not respect the format.</exception>
	private static void RespectEmailFormat(string email) {
		if (!CheckEmailFormat(email)) {
			throw new ArgumentException("Email does not respect format");
		}
	}
/// <summary>
/// Validates the format of the given password.
/// Throws an exception if the format is invalid.
/// </summary>
/// <param name="password">The password to validate.</param>
/// <exception cref="ArgumentException">Thrown when the password does not respect the format.</exception>
	public static void RespectPasswordFormat(string password) {
		if (!CheckPasswordFormat(password)) {
			throw new ArgumentException("Password does not respect format");
		}
	}
/// <summary>
/// Adjusts the fields of the given user to be in the standard format of lowercase and trimmed.
/// </summary>
/// <param name="user">The user object to adjust</param>
	public static void AdjustFields(User user) {
		user.Email = user.Email.ToLower().Trim();
		user.Username = user.Username.ToLower().Trim();
		user.Name = user.Name.ToLower().Trim();
		user.Surname = user.Surname.ToLower().Trim();
	}
/// <summary>
/// Escapes double and single quotes in a given string.
/// </summary>
/// <param name="input">The string to escape</param>
/// <returns>The escaped string</returns>
	public static string EscapeQuotes(string input) {
		if (string.IsNullOrEmpty(input)) return input;
		
		var sb = new StringBuilder(input.Length);
		foreach (char c in input) {
			switch (c){
				case '"':
					sb.Append("\\\"");
					break;
				case '\'':
					sb.Append("\\'");
					break;
				default:
					sb.Append(c);
					break;
			}
		}

		return sb.ToString();
	}
}
