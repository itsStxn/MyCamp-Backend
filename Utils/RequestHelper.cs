using System.Security.Claims;

namespace Server.Utils;

public class RequestHelper(HttpRequest request) {
	private readonly HttpRequest Request = request;

	public string GetToken() {
		var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
		if (string.IsNullOrEmpty(token)) {
			throw new BadHttpRequestException("Token not found");
		}
		return token; 
	}
	public string GetNameIdentifier(ClaimsPrincipal user) {
		var data = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(data)) {
			throw new BadHttpRequestException("User data not found in JWT token.");
		}
		return data; 
	}
}
