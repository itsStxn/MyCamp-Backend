namespace Server.Models;

public class AuthTicket {
	public required string JWT { get; set; }
	public required User User { get; set; }
}
