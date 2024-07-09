using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Server.Utils;

public class JwtHandler(string secretKey) {
	private string SecretKey { get; set; } = secretKey;
	private ClaimsPrincipal? DecodedJwt { get; set; }
	public DateTime Expiry { get; set; }

	public string GenerateJwt(object userId, string role, DateTime? exp = null) {
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
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
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
	public ClaimsPrincipal? DecodeJwt(string token){
		try {
				var tokenHandler = new JwtSecurityTokenHandler();
				var jwtToken = tokenHandler.ReadJwtToken(token);
				var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, "JWT");
				var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
				DecodedJwt = claimsPrincipal;
				return claimsPrincipal;
		} catch (Exception e) {
				Console.WriteLine(e);
				return null;
		}
	}
	public DateTime? GetDecodedExpiry() {
		var expiryClaim = DecodedJwt?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
		if (expiryClaim == null) return null;
		var unixTimeStamp = long.Parse(expiryClaim);
		return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).UtcDateTime;
	}
	public string? GetDecodedSub() {
		var subClaim = DecodedJwt?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
		return subClaim;
	}
	public string? GetDecodedRole() {
		var subClaim = DecodedJwt?.FindFirst(ClaimTypes.Role)?.Value;
		return subClaim;
	}
}
