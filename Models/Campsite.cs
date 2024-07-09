using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Campsite {
	[Key]
	public int Id { get; set; }
	public required string Loop { get; set; }
	public required  string Name { get; set; }
	public required int FacilityID { get; set; }
	public required int Capacity { get; set; }
	public required int Active { get; set; }
	public CampAttribute[]? Attributes { get; set; }
	public Equipment[]? Equipment { get; set; }
}
