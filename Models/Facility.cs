using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Facility {
	[Key]
	public int Id { get; set; }
	public string? Email { get; set; }
	public required string Name { get; set; }
	public string? Phone { get; set; }
	public string? Description { get; set; }
	public string? Directions { get; set; }
	public required decimal Latitude { get; set; }
	public required decimal Longitude { get; set; }
}
