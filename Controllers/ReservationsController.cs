using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(IReservationService reservationService, RequestHelper requestHelper) : ControllerBase {
	private readonly IReservationService ReservationService = reservationService;
	private readonly RequestHelper ReqHelper = requestHelper;

	[Authorize]
	[HttpGet]
	[ProducesResponseType(typeof(Reservation[]), StatusCodes.Status200OK)]
	public IActionResult GetReservations() {
		try {
			int userID = int.Parse(ReqHelper.GetNameIdentifier(User));
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

	[Authorize]
	[HttpPost("add")]
	[ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
	public IActionResult AddReservation([FromBody] Reservation reservation) {
		try {
			int userID = int.Parse(ReqHelper.GetNameIdentifier(User));
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
