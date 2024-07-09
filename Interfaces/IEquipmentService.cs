using Server.Models;

namespace Server.Interfaces;

public interface IEquipmentService {
	Equipment[] GetEquipment();
	Equipment[] GetEquipmentByCampsite(int campsiteId);
}
