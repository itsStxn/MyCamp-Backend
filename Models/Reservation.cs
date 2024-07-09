using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Reservation {
	[Key]	
	public int Id { get; set; }
	public required DateTime CheckIn { get; set; }
	public required DateTime CheckOut { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required int UserID { get; set; }
	public required int CampsiteID { get; set; }
}
