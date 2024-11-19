using System.Security.Claims;

namespace Server.Utils;

public class RequestHelper(HttpRequest request) {
	private readonly HttpRequest Request = request;

/// <summary>
/// Retrieves the JWT token from the Authorization header of the request.
/// If the Authorization header is not present or does not contain a valid token,
/// an exception is thrown.
/// </summary>
/// <returns>The JWT token.</returns>
/// <exception cref="BadHttpRequestException">Thrown if the token is not found.</exception>
	public string GetToken() {
		var token = Request.Headers.Authorization
		.ToString()
		.Replace("Bearer ", "");
		if (string.IsNullOrEmpty(token)) {
			throw new BadHttpRequestException("Token not found");
		}
		return token; 
	}
/// <summary>
/// Retrieves the name identifier claim from the specified <see cref="ClaimsPrincipal"/>.
/// </summary>
/// <param name="user">The <see cref="ClaimsPrincipal"/> from which to extract the name identifier.</param>
/// <returns>The name identifier as a string.</returns>
/// <exception cref="BadHttpRequestException">Thrown if the name identifier is not found in the claims.</exception>
	public static string GetNameIdentifier(ClaimsPrincipal user) {
		var data = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(data)) {
			throw new BadHttpRequestException("User data not found in JWT token.");
		}
		return data;
	}
}
