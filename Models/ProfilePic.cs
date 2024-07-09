using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class ProfilePic {
	[Key]
	public int Id { get; set; }
	public required string Location { get; set; }
}
