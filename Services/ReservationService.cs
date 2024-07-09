using Server.Interfaces;
using Server.Models;
using System.Data;
using Dapper;

namespace Server.Services;

public class ReservationService(IDbConnection db, Lazy<ICampsiteService> campsiteService) : IReservationService {
	private readonly IDbConnection Db = db;
	private readonly Lazy<ICampsiteService> CampsiteService = campsiteService;

	public Reservation[] GetReservations(int userID) {
		string selectQuery = SelectReservationsQuery();
		var reservations = Db.Query<Reservation>(selectQuery, new { userID });
		return reservations.ToArray();
	}
	public Reservation[] GetReservationsByCampsite(int campsiteID) {
		string selectQuery = SelectReservationsByCampsiteQuery();
		var reservations = Db.Query<Reservation>(selectQuery, new { campsiteID });
		return reservations.ToArray();
	}
	public bool AddReservation(Reservation reservation) {
		ValidateCampsite(reservation);
		ValidateReservation(reservation);
		string insertQuery = InsertReservationQuery();
		int res = Db.Execute(insertQuery, reservation);
		return res > 0;
	}
	public bool DeleteReservation(int reservationID) {
		string deleteQuery = DeleteReservationQuery();
		int res = Db.Execute(deleteQuery, new { reservationID });
		return res > 0;
	}
	public bool DeleteReservations(int campsiteID, IDbTransaction? trans = null) {
		string deleteQuery = DeleteReservationsQuery();
		int res = Db.Execute(deleteQuery, new { campsiteID }, trans);
		return res > 0;
	}

	#region Aid Functions

	private void ValidateCampsite(Reservation reservation) {
		_ = CampsiteService.Value.GetCampsite(reservation.CampsiteID)
		?? throw new KeyNotFoundException("Campsite not found");
	}
	private void ValidateReservation(Reservation reservation) {
		if (!CorrectCheckInCheckOut(reservation) || IsOverlapping(reservation)) {
			throw new InvalidOperationException("Invalid reservation");
		}
	}
	private static bool CorrectCheckInCheckOut(Reservation reservation) {
		return 
		reservation.CheckIn >= DateTime.Today &&
		DateTime.Today.AddMonths(2) >= reservation.CheckOut &&
		reservation.CheckOut >= reservation.CheckIn;
	}
	private bool IsOverlapping(Reservation reservation) {
		string selectQuery = SelectOverlapReservationsQuery();
		var parameters = OverlapReservationsParams(reservation);
		Reservation[] overlaps = Db.Query<Reservation>(selectQuery, parameters).ToArray();
		return overlaps.Length > 0;
	}
	private static object OverlapReservationsParams(Reservation reservation) {
		return new { 
			userID = reservation.UserID, 
			checkIn = reservation.CheckIn, 
			checkOut = reservation.CheckOut 
		};
	}

	#endregion

	#region Queries

	private static string SelectReservationsByCampsiteQuery() {
		return "SELECT * FROM reservations WHERE campsiteID = @campsiteID";
	}
	private static string InsertReservationQuery() {
		string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
		return @$"
			INSERT INTO reservations 
			(userID, campsiteID, checkIn, checkOut, createdAt) 
			VALUES (@userID, @campsiteID, @checkIn, @checkOut, '{timestamp}');";
	}
	private static string SelectOverlapReservationsQuery() {
		return @"
			SELECT * FROM reservations 
			WHERE userID = @userID
			AND (
				(checkIn <= @checkOut AND @checkIn <= checkOut)
				OR (@checkIn <= checkIn AND checkOut <= @checkOut)
			)";
	}		
	private static string SelectReservationsQuery() {
		return "SELECT * FROM reservations WHERE userID = @userID";
	}
	private static string DeleteReservationQuery() {
		return "DELETE FROM reservations WHERE id = @reservationID";
	}
	private static string DeleteReservationsQuery() {
		return "DELETE FROM reservations WHERE campsiteID = @campsiteID"; 
	}

	#endregion
}
