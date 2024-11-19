using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(IReservationService reservationService) : ControllerBase {
	private readonly IReservationService ReservationService = reservationService;

/// <summary>
/// Retrieves all reservations made by the user.
/// </summary>
/// <remarks>
/// This endpoint requires a valid JWT token to be provided in the request's Authorization header.
/// </remarks>
/// <returns>An array of reservations made by the user.</returns>
/// <response code="200">The operation was successful.</response>
/// <response code="404">The user could not be found in the database.</response>
/// <response code="500">An unexpected error occurred while attempting to fulfill the request.</response>
	[Authorize]
	[HttpGet]
	[ProducesResponseType(typeof(Reservation[]), StatusCodes.Status200OK)]
	public IActionResult GetReservations() {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			Reservation[] reservations = ReservationService.GetReservations(userID);
			return Ok(reservations);
		}
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		}
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching reservations: {e.Message}");
		}
	}

/// <summary>
/// Adds a new reservation to the database.
/// </summary>
/// <remarks>
/// This endpoint requires a valid JWT token to be provided in the request's Authorization header.
/// </remarks>
/// <param name="reservation">The reservation to add, including its check-in and check-out dates, campsite ID, and number of guests.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <response code="201">The reservation was added successfully.</response>
/// <response code="400">The reservation is invalid.</response>
/// <response code="409">The reservation conflicts with an existing reservation.</response>
/// <response code="500">An unexpected error occurred while attempting to fulfill the request.</response>
	[Authorize]
	[HttpPost("add")]
	[ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
	public IActionResult AddReservation([FromBody] Reservation reservation) {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			reservation.UserID = userID;
			bool created = ReservationService.AddReservation(reservation);
			if (!created) return BadRequest("Failed to add reservation");
			return Ok("Reservation added successfully");
		}
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues adding reservation: {e.Message}");
		}
	}

/// <summary>
/// Deletes a reservation from the database.
/// </summary>
/// <remarks>
/// This endpoint requires a valid JWT token to be provided in the request's Authorization header.
/// </remarks>
/// <param name="reservationID">The ID of the reservation to delete.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <response code="200">The reservation was deleted successfully.</response>
/// <response code="404">The reservation to delete was not found.</response>
/// <response code="500">An unexpected error occurred while attempting to fulfill the request.</response>
	[Authorize]
	[HttpDelete("{reservationID}")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DeleteReservation(int reservationID) {
		try {
			bool deleted = ReservationService.DeleteReservation(reservationID);
			if (!deleted) return NotFound("Reservation not found");
			return Ok("Reservation deleted successfully");
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues deleting reservation: {e.Message}");
		}
	}

/// <summary>
/// Deletes all reservations associated with a specific campsite.
/// </summary>
/// <remarks>
/// This endpoint requires a valid JWT token to be provided in the request's Authorization header.
/// </remarks>
/// <param name="campsiteID">The ID of the campsite whose reservations are to be deleted.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <response code="200">Reservations were deleted successfully.</response>
/// <response code="404">The campsite to delete reservations for was not found.</response>
/// <response code="500">An unexpected error occurred while attempting to fulfill the request.</response>
	[Authorize]
	[HttpDelete("campsites/{campsiteID}")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DeleteReservations(int campsiteID) {
		try {
			bool deleted = ReservationService.DeleteReservations(campsiteID);
			if (!deleted) return NotFound("Campsite not found");
			return Ok("Reservations deleted successfully");
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues deleting reservation: {e.Message}");
		}
	}
}
