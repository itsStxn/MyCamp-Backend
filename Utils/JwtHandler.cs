using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Server.Utils;

public class JwtHandler(string secretKey) {
	private ClaimsPrincipal? DecodedJwt { get; set; }
	private readonly string SecretKey = secretKey;
	public DateTime Expiry { get; set; }

/// <summary>
/// Generates a JWT token for a given user
/// </summary>
/// <param name="userId">The user's ID</param>
/// <param name="role">The user's role</param>
/// <param name="exp">The optional expiration time for the token</param>
/// <returns>A JWT token</returns>
	public string GenerateJwt(object userId, string role, DateTime? exp = null) {
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
		var sign = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
		string data = userId?.ToString() ?? string.Empty;

		var claims = new[] {
			new Claim(JwtRegisteredClaimNames.Sub, data),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(ClaimTypes.Role, role)
		};

		Expiry = exp ?? DateTime.Now.AddMinutes(30);
		var token = new JwtSecurityToken(
			issuer: "MyCamp",
			audience: "users",
			claims: claims,
			expires: Expiry,
			signingCredentials: sign
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
/// <summary>
/// Decodes a JWT token into a <see cref="ClaimsPrincipal"/>.
/// </summary>
/// <param name="token">The JWT token to decode</param>
/// <returns>The decoded <see cref="ClaimsPrincipal"/>, or null if the decode fails</returns>
	public ClaimsPrincipal? DecodeJwt(string token) {
		try {
			var tokenHandler = new JwtSecurityTokenHandler();
			var jwtToken = tokenHandler.ReadJwtToken(token);
			var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, "JWT");

			DecodedJwt = new ClaimsPrincipal(claimsIdentity);
			return DecodedJwt;
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return null;
		}
	}
/// <summary>
/// Gets the decoded expiry date and time from the JWT token, or null if there is no token or the token lacks an expiry claim.
/// </summary>
/// <returns>The decoded expiry date and time, or null</returns>
	public DateTime? GetDecodedExpiry() {
		var expiryClaim = DecodedJwt?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
		if (expiryClaim == null) return null;

		var unixTimeStamp = long.Parse(expiryClaim);
		return DateTimeOffset
		.FromUnixTimeSeconds(unixTimeStamp).UtcDateTime;
	}
/// <summary>
/// Retrieves the subject (sub) claim from the decoded JWT token.
/// </summary>
/// <returns>The subject claim value as a string, or null if the subject claim is not present or the token has not been decoded.</returns>
	public string? GetDecodedSub() {
		return DecodedJwt?
		.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
	}
}
