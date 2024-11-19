using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using System.Data;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampsitesController(ICampsiteService campsiteService) : ControllerBase {
	private readonly ICampsiteService CampsiteService = campsiteService;

/// <summary>
/// Adds a new campsite to the database.
/// </summary>
/// <param name="site">The campsite to add, including its attributes and equipment.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="InvalidOperationException">Thrown when the campsite's name is already in use.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	[Authorize("Admin")]
	[HttpPost("add")]
	[ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
	public IActionResult AddCampsite([FromBody] AddCampsite site) {
		try {
			bool created = CampsiteService.AddCampsite(site.Campsite, site.Attributes, site.Equipment);
			if (!created) return BadRequest("Failed to add campsite");
			return Ok("Campsite added successfully");
		}
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues adding campsite: {e.Message}");
		}
	}

/// <summary>
/// Enables a campsite, allowing it to be booked again.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to enable.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="Exception">Thrown for any general exceptions.</exception>
	[Authorize("Admin")]
	[HttpPut("enable")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult EnableCampsite([FromQuery] int campsiteID) {
		try {
			bool enabled = CampsiteService.EnableCampsite(campsiteID);
			if (!enabled) return BadRequest("Campsite not found");
			return Ok("Campsite enabled successfully");
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues enabling campsite: {e.Message}");
		}
	}

/// <summary>
/// Disables a campsite, preventing it from being booked again.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to disable.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="KeyNotFoundException">Thrown if the campsite does not exist.</exception>
/// <exception cref="InvalidOperationException">Thrown if the campsite is already disabled.</exception>
/// <exception cref="Exception">Thrown for any general exceptions.</exception>
	[Authorize("Admin")]
	[HttpPut("disable")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DisableCampsite([FromQuery] int campsiteID) {
		try {
			bool disabled = CampsiteService.DisableCampsite(campsiteID);
			if (!disabled) return NotFound("Campsite not found");
			return Ok("Campsite disabled successfully");
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
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues disabling campsite: {e.Message}");
		}
	}

/// <summary>
/// Deletes a campsite from the database.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to delete.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="KeyNotFoundException">Thrown if the campsite does not exist.</exception>
/// <exception cref="InvalidOperationException">Thrown for any invalid operations during deletion.</exception>
/// <exception cref="Exception">Thrown for any general exceptions.</exception>
	[Authorize("Admin")]
	[HttpDelete("delete")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DeleteCampsite([FromQuery] int campsiteID) {
		try {
			bool deleted = CampsiteService.DeleteCampsite(campsiteID);
			if (!deleted) return NotFound("Campsite not found");
			return Ok("Campsite deleted successfully");
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
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues deleting campsite: {e.Message}");
		}
	}

/// <summary>
/// Updates the capacity of a given campsite.
/// </summary>
/// <param name="camp">Object containing the campsite ID and the new capacity.</param>
/// <returns>A status code indicating the success of the operation, with a message if the operation failed.</returns>
/// <exception cref="KeyNotFoundException">Thrown if the campsite does not exist.</exception>
/// <exception cref="DataException">Thrown if the capacity is invalid.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	[Authorize("Admin")]
	[HttpPost("setCapacity")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult UpdateCapacity([FromBody] UpdateCampCapacity camp) {
		try {
			bool updated = CampsiteService.UpdateCapacity(camp.CampsiteID, camp.Capacity);
			if (!updated) return BadRequest("Failed to update capacity");
			return Ok("Capacity updated successfully");
		}
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		}
		catch (DataException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues updating capacity: {e.Message}");
		}
	}

/// <summary>
/// Retrieves a dictionary of available dates for a given campsite.
/// </summary>
/// <param name="campsiteID">The ID of the campsite to retrieve availabilities for.</param>
/// <returns>A dictionary where the keys are the available dates in the format "YYYY-MM-DD" and the values are boolean indicating whether the date is available (true) or unavailable (false).</returns>
/// <exception cref="InvalidOperationException">Thrown if the campsite does not exist.</exception>
/// <exception cref="Exception">Thrown for any other general exceptions.</exception>
	[Authorize]
	[HttpGet("{campsiteID}/availabilities")]
	[ProducesResponseType(typeof(Dictionary<string, bool>), StatusCodes.Status200OK)]
	public IActionResult GetAvailabilities(int campsiteID) {
		try {
			return Ok(CampsiteService.GetAvailabilities(campsiteID));
		}
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return NotFound( "Campsite does not exist");
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching availabilities: {e.Message}");
		}
	}
}
