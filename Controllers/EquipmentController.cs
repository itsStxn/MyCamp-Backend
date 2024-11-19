using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController(IEquipmentService equipmentService) : ControllerBase {
	private readonly IEquipmentService EquipmentService = equipmentService;

/// <summary>
/// Retrieves all equipment from the database.
/// </summary>
/// <returns>An IActionResult containing an array of <see cref="Equipment"/> objects if successful, or a 500 status code if an error occurs.</returns>
/// <response code="200">Returns the array of equipment.</response>
/// <response code="500">If there was an error fetching the equipment.</response>
	[Authorize]
	[HttpGet]
	[ProducesResponseType(typeof(Equipment[]), StatusCodes.Status200OK)]
	public IActionResult GetEquipment() {
		try {
			Equipment[] equipment = EquipmentService.GetEquipment();
			return Ok(equipment);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching equipment: {e.Message}");
		}
	}
}
