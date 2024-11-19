using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase {
	private readonly IUserService UserService = userService;

/// <summary>
/// Updates the authenticated user's profile information.
/// If the user ID is valid and the user update is successful, returns an HTTP 200 OK response.
/// If the user ID is invalid or the user already exists, returns an HTTP 400 Bad Request response.
/// If any other error occurs, returns an HTTP 500 Internal Server Error response.
/// </summary>
/// <param name="user">The User object containing the updated details.</param>
/// <returns>A response indicating the success or failure of the user update operation.</returns>
/// <response code="200">User updated successfully</response>
/// <response code="400">Failed to update user or user already exists</response>
/// <response code="500">Issues updating user</response>
	[Authorize]
	[HttpPut("current/update")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult UpdateUser([FromBody] User user) {
		try {
			user.Id = int.Parse(RequestHelper.GetNameIdentifier(User));
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

/// <summary>
/// Disables the authenticated user by setting the user's status in the database to inactive.
/// If the user ID is valid and the user disable is successful, returns an HTTP 200 OK response.
/// If the user ID is invalid or any other error occurs, returns an HTTP 500 Internal Server Error response.
/// </summary>
/// <returns>A response indicating the success or failure of the user disable operation.</returns>
/// <response code="200">User disabled successfully</response>
/// <response code="500">Issues disabling user</response>
	[Authorize]
	[HttpPut("current/disable")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DisableUser() {
		try {
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
			bool disabled = UserService.DisableUser(userID);
			if (!disabled) return BadRequest("Failed to disable user");
			return Ok("User removed successfully");
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues disabling user: {e.Message}");
		}
	}

/// <summary>
/// Elevates a user to an admin role.
/// </summary>
/// <param name="userID">The ID of the user to elevate.</param>
/// <returns>A success message if the user was elevated successfully, or an HTTP 500 Internal Server Error response in case of an exception.</returns>
/// <response code="200">User elevated successfully</response>
/// <response code="404">User not found</response>
/// <response code="500">Issues elevating user</response>
	[Authorize("Admin")]
	[HttpPost("elevate")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult ElevateUser([FromQuery] int userID) {
		try {
			UserService.ElevateUser(userID);
			return Ok("User elevated successfully");
		} 
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues elevating user: {e.Message}");
		}
	}

/// <summary>
/// Demotes a user from an admin role to a regular user.
/// </summary>
/// <param name="userID">The ID of the user to demote.</param>
/// <returns>A success message if the user was demoted successfully, or an HTTP 404 Not Found response if the user does not exist.
/// Returns an internal server error response in case of an exception.</returns>
/// <response code="200">User demoted successfully</response>
/// <response code="404">User not found</response>
/// <response code="500">Issues demoting user</response>
	[Authorize("Admin")]
	[HttpDelete("demote")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult DemoteUser([FromQuery] int userID) {
		try {
			UserService.DemoteUser(userID);
			return Ok("User demoted successfully");
		} 
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues demoting user: {e.Message}");
		}
	}
}
