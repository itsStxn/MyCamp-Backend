using Server.Interfaces;

namespace Server.Services;

public class SecretKeyService : ISecretKeyService{
	private readonly string? SecretKey = Environment.GetEnvironmentVariable("SecretKey");
	private readonly string? GmailAppKey = Environment.GetEnvironmentVariable("GmailAppKey");

/// <summary>
/// Retrieves the secret key used for generating JSON Web Tokens.
/// </summary>
/// <returns>The secret key.</returns>
/// <exception cref="KeyNotFoundException">Thrown when the secret key is not found.</exception>
	public string GetSecretKey() {
		return SecretKey ?? throw new KeyNotFoundException("Secret key not found");
	}
/// <summary>
/// Retrieves the Gmail application key used for authentication.
/// </summary>
/// <returns>The Gmail app key.</returns>
/// <exception cref="KeyNotFoundException">Thrown when the Gmail app key is not found.</exception>
	public string GetGmailAppKey() {
		return GmailAppKey ?? throw new KeyNotFoundException("Gmail app key not found");
	}
}
