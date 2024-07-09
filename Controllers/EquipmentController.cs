using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController(IEquipmentService equipmentService) : ControllerBase {
	private readonly IEquipmentService EquipmentService = equipmentService;

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
