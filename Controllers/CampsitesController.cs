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

	[Authorize]
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

	[Authorize]
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

	[Authorize]
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

	[Authorize]
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

	[Authorize]
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
