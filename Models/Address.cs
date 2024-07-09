using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Address {
	[Key]
	public int Id { get; set; }
	public required string? Type { get; set; }
	public required string? Country { get; set; }
	public required string? StateCode { get; set; }
	public required string? TaxCode { get; set; }
	public string? City { get; set; }
	public required string? Street1 { get; set; }
	public string? Street2 { get; set; }
	public string? Street3 { get; set; }
	public required int FacilityID { get; set; }
}
