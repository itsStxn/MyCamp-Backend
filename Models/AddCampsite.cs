namespace Server.Models;

public class AddCampsite {
	public required Campsite Campsite { get; set; }
	public required CampAttribute[] Attributes { get; set; }
	public required Equipment[] Equipment { get; set; }
}
