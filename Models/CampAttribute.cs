using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class CampAttribute {
	[Key]
	public int Id { get; set; }
	public required string Name { get; set; }
	public required string Value { get; set; }
}
