using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Comment {
	[Key]
	public int Id { get; set; }
	public required string Content { get; set; }
	public required int UserID { get; set; }
	public required int FacilityID { get; set; }
	public DateTime? CreatedAt { get; set; }
	public LikeInfo? LikeInfo { get; set; }
	public Reply[]? Replies { get; set; }
}
