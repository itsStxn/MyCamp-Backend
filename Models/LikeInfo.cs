namespace Server.Models;

public class LikeInfo {
	public required int Likes { get; set; }
	public required int Dislikes { get; set; }
	public required bool Liked { get; set; }
	public required bool Disliked { get; set; }
}
