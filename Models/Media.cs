using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Media {
	[Key]
	public int Id { get; set; }
	public string? Title { get; set; }
	public string? Description { get; set; }
	public string? Credits { get; set; }
	public required string? Url { get; set; }
}
