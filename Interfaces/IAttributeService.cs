using Server.Models;

namespace Server.Interfaces;

public interface IAttributeService {
	CampAttribute[] GetAttributes();
	CampAttribute[] GetAttributesByCampsite(int campsiteID);
}
