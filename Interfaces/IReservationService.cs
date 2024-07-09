using Server.Models;
using System.Data;

namespace Server.Interfaces;

public interface IReservationService {
	Reservation[] GetReservations(int userID);
	Reservation[] GetReservationsByCampsite(int campsiteID);
	bool AddReservation(Reservation reservation);
	bool DeleteReservation(int reservationID);
	bool DeleteReservations(int campsiteID, IDbTransaction? trans = null);
}
