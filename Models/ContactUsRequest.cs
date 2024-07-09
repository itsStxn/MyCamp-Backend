namespace Server.Models;

public class ContactUsRequest {
	public required string Email { get; set; }
	public required string Subject { get; set; }
	public required string Message { get; set; }
}
