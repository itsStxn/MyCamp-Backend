using Server.Interfaces;

namespace Server.Services;

public class SecretKeyService : ISecretKeyService{
	private readonly string? SecretKey = Environment.GetEnvironmentVariable("SecretKey");
	private readonly string? GmailAppKey = Environment.GetEnvironmentVariable("GmailAppKey");
	public string GetSecretKey() {
		return SecretKey ?? throw new KeyNotFoundException("Secret key not found");
	}
	public string GetGmailAppKey() {
		return GmailAppKey ?? throw new KeyNotFoundException("Gmail app key not found");
	}
}
