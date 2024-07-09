using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Activity {
	[Key]
	public int Id { get; set; }
	public required string Name { get; set; }
}
