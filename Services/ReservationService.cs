using Server.Interfaces;
using Server.Models;
using System.Data;
using Dapper;

namespace Server.Services;

public class ReservationService(IDbConnection db, Lazy<ICampsiteService> campsiteService) : IReservationService {
	private readonly IDbConnection Db = db;
	private readonly Lazy<ICampsiteService> CampsiteService = campsiteService;

/// <summary>
/// Retrieves all reservations made by the given user.
/// </summary>
/// <param name="userID">The ID of the user to retrieve reservations for.</param>
/// <returns>An array of reservations made by the given user.</returns>
	public Reservation[] GetReservations(int userID) {
		string selectQuery = SelectReservationsQuery();
		var reservations = Db.Query<Reservation>(selectQuery, new { userID });
		return reservations.ToArray();
	}
/// <summary>
/// Retrieves all reservations made for the given campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to retrieve reservations for.</param>
/// <returns>An array of reservations made for the given campsite.</returns>
	public Reservation[] GetReservationsByCampsite(int campsiteID) {
		string selectQuery = SelectReservationsByCampsiteQuery();
		var reservations = Db.Query<Reservation>(selectQuery, new { campsiteID });
		return reservations.ToArray();
	}
/// <summary>
/// Adds a new reservation to the database.
/// </summary>
/// <param name="reservation">The reservation to add.</param>
/// <returns>True if the reservation was successfully added, false otherwise.</returns>
/// <exception cref="InvalidOperationException">Thrown when the reservation's campsite does not exist.</exception>
/// <exception cref="DataException">Thrown when there is an issue with database operations.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	public bool AddReservation(Reservation reservation) {
		ValidateCampsite(reservation);
		ValidateReservation(reservation);
		string insertQuery = InsertReservationQuery();
		int res = Db.Execute(insertQuery, reservation);
		return res > 0;
	}
/// <summary>
/// Deletes a reservation from the database.
/// </summary>
/// <param name="reservationID">The ID of the reservation to delete.</param>
/// <returns>True if the reservation was successfully deleted, false otherwise.</returns>
/// <exception cref="DataException">Thrown when there is an issue with database operations.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	public bool DeleteReservation(int reservationID) {
		string deleteQuery = DeleteReservationQuery();
		int res = Db.Execute(deleteQuery, new { reservationID });
		return res > 0;
	}
/// <summary>
/// Deletes all reservations associated with the specified campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to delete reservations for.</param>
/// <param name="trans">The transaction to use for database operations. If null, the operation is executed without a transaction.</param>
/// <returns>True if the reservations were successfully deleted, false otherwise.</returns>
	public bool DeleteReservations(int campsiteID, IDbTransaction? trans = null) {
		string deleteQuery = DeleteReservationsQuery();
		int res = Db.Execute(deleteQuery, new { campsiteID }, trans);
		return res > 0;
	}

	#region Aid Functions

/// <summary>
/// Validates that the given reservation's campsite exists.
/// </summary>
/// <param name="reservation">The reservation to validate.</param>
/// <exception cref="KeyNotFoundException">Thrown when the campsite does not exist.</exception>
	private void ValidateCampsite(Reservation reservation) {
		_ = CampsiteService.Value.GetCampsite(reservation.CampsiteID)
		?? throw new KeyNotFoundException("Campsite not found");
	}
/// <summary>
/// Validates that the given reservation is valid.
/// Checks that the reservation does not overlap with existing reservations
/// and that the check-in and check-out dates are valid.
/// </summary>
/// <param name="reservation">The reservation to validate.</param>
/// <exception cref="InvalidOperationException">Thrown when the reservation is invalid.</exception>
	private void ValidateReservation(Reservation reservation) {
		if (!CorrectCheckInCheckOut(reservation) || IsOverlapping(reservation)) {
			throw new InvalidOperationException("Invalid reservation");
		}
	}
/// <summary>
/// Checks that the given reservation's check-in and check-out dates are valid.
/// A valid reservation is one where the check-in date is today or later,
/// the check-out date is within the next 2 months, and the check-in date is
/// before the check-out date.
/// </summary>
/// <param name="reservation">The reservation to check.</param>
/// <returns>True if the reservation's dates are valid, false otherwise.</returns>
	private static bool CorrectCheckInCheckOut(Reservation reservation) {
		return 
		reservation.CheckIn >= DateTime.Today &&
		DateTime.Today.AddMonths(2) >= reservation.CheckOut &&
		reservation.CheckOut >= reservation.CheckIn;
	}
/// <summary>
/// Checks if the given reservation overlaps with any existing reservations
/// made by the same user.
/// </summary>
/// <param name="reservation">The reservation to check.</param>
/// <returns>True if the reservation overlaps with an existing reservation,
/// false otherwise.</returns>
	private bool IsOverlapping(Reservation reservation) {
		string selectQuery = SelectOverlapReservationsQuery();
		var parameters = OverlapReservationsParams(reservation);
		Reservation[] overlaps = Db.Query<Reservation>(selectQuery, parameters).ToArray();
		return overlaps.Length > 0;
	}
/// <summary>
/// Creates an anonymous object containing parameters required to check for overlapping reservations.
/// </summary>
/// <param name="reservation">The reservation for which to generate overlap parameters.</param>
/// <returns>An anonymous object with userID, checkIn, and checkOut fields.</returns>
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
