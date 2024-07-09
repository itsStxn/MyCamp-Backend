using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class LikeState {
	[Key]
	public int Id { get; set; }
	public required bool Liked { get; set; }
	public required bool Disliked { get; set; }
	public required int UserID { get; set; }
	public required int CommentID { get; set; }
}
