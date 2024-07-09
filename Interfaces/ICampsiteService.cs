using Server.Models;

namespace Server.Interfaces;

public interface ICampsiteService {
	Campsite[] GetCampsites(int facilityID);
	Campsite? GetCampsite(int campsiteID);
	bool AddCampsite(Campsite campsite, CampAttribute[] attributes, Equipment[] equipment);
	bool DeleteCampsite(int campsiteID);
	bool EnableCampsite(int campsiteID);
	bool DisableCampsite(int campsiteID);
	bool UpdateCapacity(int campsiteID, int capacity);
	Dictionary<string, bool> GetAvailabilities(int campsiteID);
}
