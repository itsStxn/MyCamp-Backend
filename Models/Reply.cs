namespace Server.Models;

public class Reply : Comment {
	public required int Thread { get; set; }
	public required int ReplyTo { get; set; }
}
