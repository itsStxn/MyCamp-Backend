using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService, RequestHelper requestHelper) : ControllerBase {
	private readonly IUserService UserService = userService;
	private readonly RequestHelper ReqHelper = requestHelper;

	[Authorize]
	[HttpPut("current/update")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult UpdateUser([FromBody] User user) {
		try {
			user.Id = int.Parse(ReqHelper.GetNameIdentifier(User));
			bool edited = UserService.UpdateUser(user);
			if (!edited) return BadRequest("Failed to update user");
			return Ok("User updated successfully");
		} 
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return BadRequest("User already exists");
		}
		catch (ArgumentException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues updating user: {e.Message}");
		}
	}

	[Authorize]
	[HttpPut("current/disable")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DisableUser() {
		try {
			int userID = int.Parse(ReqHelper.GetNameIdentifier(User));
			bool disabled = UserService.DisableUser(userID);
			if (!disabled) return BadRequest("Failed to disable user");
			return Ok("User removed successfully");
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues disabling user: {e.Message}");
		}
	}
}
