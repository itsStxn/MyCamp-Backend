using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class TokenTicket {
	[Key]
	public int Id { get; set; }
	public required string Token { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required DateTime Expiry { get; set; }
	public required int UserId { get; set; }
}
