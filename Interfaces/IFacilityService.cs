using Server.Models;

namespace Server.Interfaces;

public interface IFacilityService {
	Facility[] GetFacilities(string[]? activities = null, string[]? states = null, string? rating = null);
	Facility? GetFacility(int facilityID);
	Campsite[] GetCampsites(int facilityID);
	Address[] GetAddresses(int facilityID);
	Activity[] GetActivities(int facilityID);
	Media[] GetMedia(int facilityID);
	Facility[] GetSavedFacilities(int userID);
	bool SaveFacility(int facilityID, int userID);
	bool RateFacility(int score, int facilityID, int userID);
	FacilityScore? GetUserRating(int userID, int facilityID);
	FacilityRating GetFacilityRating(int facilityID);
	Comment[] GetComments(int facilityID, int userID);
}
