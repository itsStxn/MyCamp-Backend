using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttributesController(IAttributeService attributeService) : ControllerBase {
	private readonly IAttributeService AttributeService = attributeService;

	[Authorize]
	[HttpGet]
	[ProducesResponseType(typeof(CampAttribute[]), StatusCodes.Status200OK)]
	public IActionResult GetAttributes() {
		try {
			CampAttribute[] attributes = AttributeService.GetAttributes();
			return Ok(attributes);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues fetching attributes: {e.Message}");
		}
	}
}
