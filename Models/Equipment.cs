using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Equipment {
	[Key]
	public int Id { get; set; }
	public required string Name { get; set; }
}
