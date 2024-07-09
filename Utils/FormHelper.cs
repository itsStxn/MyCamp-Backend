using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Server.Models;

namespace Server.Utils;

public class FormHelper {
	private readonly PasswordHasher<object> passwordHasher;
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

	public FormHelper(){
		passwordHasher = new PasswordHasher<object>();
	}

	public string? GetStateCode(string stateName){
		StateToCodeMap.TryGetValue(stateName, out string? stateCode);
		return stateCode;
	}	
	public string HashPassword(string password) {
		var hashedPassword = passwordHasher.HashPassword(new {}, password);
		return hashedPassword;
	}
	public bool VerifyPassword(string hashedPassword, string password) {
		var verificationResult = passwordHasher.VerifyHashedPassword(new {}, hashedPassword, password);
		return verificationResult == PasswordVerificationResult.Success;
	}
	public bool CheckNameFormat(string name, string surname) {
		string pattern = @"^[a-zA-Z]+$";
		return Regex.IsMatch(name, pattern) 
		&& Regex.IsMatch(surname, pattern);
	}
	public bool CheckUsernameFormat(string username) {
		string pattern = @"^[a-zA-Z0-9_]+$";
		return Regex.IsMatch(username, pattern);
	}
	public bool CheckEmailFormat(string email) {
		string pattern = @"^([a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})$";
		return Regex.IsMatch(email, pattern);
	}
	public bool CheckPasswordFormat(string password) {
		string pattern = @"^(?=.*[0-9])(?=.*[!@#*?])(?=.*[A-Z])(?=.*[a-z]).*[^\s'"";$%<>|`^~\\]+$";
		return Regex.IsMatch(password, pattern) && password.Length >= 8;
	}
	public void RespectFormFormats(User user) {
		RespactNameFormat(user.Name, user.Surname);
		RespectUsernameFormat(user.Username);
		RespectEmailFormat(user.Email);
		RespectPasswordFormat(user.Password);
	}
	public void RespactNameFormat(string name, string surname) {
		if (!CheckNameFormat(name, surname)) {
			throw new ArgumentException("Name or surname does not respect format");
		}
	}
	public void RespectUsernameFormat(string username) {
		if (!CheckUsernameFormat(username)) {
			throw new ArgumentException("Username does not respect format");
		}
	}
	public void RespectEmailFormat(string email) {
		if (!CheckEmailFormat(email)) {
			throw new ArgumentException("Email does not respect format");
		}
	}
	public void RespectPasswordFormat(string password) {
		if (!CheckPasswordFormat(password)) {
			throw new ArgumentException("Password does not respect format");
		}
	}
	public void AdjustFields(User user) {
		user.Email = user.Email.ToLower().Trim();
		user.Username = user.Username.ToLower().Trim();
		user.Name = user.Name.ToLower().Trim();
		user.Surname = user.Surname.ToLower().Trim();
	}
	public string EscapeQuotes(string input) {
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
