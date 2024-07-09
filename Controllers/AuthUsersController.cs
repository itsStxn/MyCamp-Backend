using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;
using Server.Utils;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthUsersController(IAuthUserService authUserService, RequestHelper requestHelper) : ControllerBase {
	private readonly IAuthUserService AuthUserService = authUserService;
	private readonly RequestHelper ReqHelper = requestHelper;
	
	[HttpPost("signup")]
	[ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
	public IActionResult RegisterUser([FromBody] User user) {
		try {
			bool created = AuthUserService.RegisterUser(user);
			if (!created) return BadRequest("Failed to mail confirmation link");
			return Ok("User added successfully");
		} 
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		} 
		catch (UnauthorizedAccessException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		} 
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return Conflict(e.Message);
		} 
		catch (ArgumentException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues adding user: {e.Message}");
		}
	}

	[HttpPost("login")]
	[ProducesResponseType(typeof(AuthTicket), StatusCodes.Status200OK)]
	public IActionResult AuthenticateUser([FromBody] AuthUser user) {
		try {
			AuthTicket? authenticatedUser = AuthUserService.AuthenticateUser(user);
			if (authenticatedUser == null) return Unauthorized("Wrong password");
			return Ok(authenticatedUser);
		} 
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		} 
		catch (ArgumentException e) {
			Console.WriteLine(e);
			return Unauthorized(e.Message);
		} 
		catch (UnauthorizedAccessException e) {
			Console.WriteLine(e);
			return Unauthorized(e.Message);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues logging in user: {e.Message}");
		}
	}

	[HttpPost("mailCredentials")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult MailCredentialsSetter([FromBody] MailCredentials cred) {
		try {
			string? sent = AuthUserService.MailCredentialsSetter(cred);
			if (sent == null) return BadRequest("Failed to mail credentials setter");
			return Ok($"Link sent successfully to {sent}");
		} 
		catch (KeyNotFoundException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues sending credentials: {e.Message}");
		}
	}

	[Authorize]
	[HttpPut("changePassword")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult ChangePassword([FromBody] ChangePassword req) {
		try {
			var token = ReqHelper.GetToken();
			int userID = int.Parse(ReqHelper.GetNameIdentifier(User));
			bool changed = AuthUserService.ChangePassword(token, userID, req.NewPassword);
			if (!changed) return BadRequest("Failed to change password or delete token");
			return Ok("Password changed successfully");
		} 
		catch (BadHttpRequestException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		} 
		catch (UnauthorizedAccessException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		} 
		catch (InvalidOperationException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		} 
		catch (ArgumentException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues changing password: {e.Message}");
		}
	}

	[Authorize]
	[HttpPut("activateAccount")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult ActivateAccount() {
		try {
			var token = ReqHelper.GetToken();
			bool activated = AuthUserService.ActivateAccount(token);
			if (!activated) return BadRequest("Failed to activate account or delete token");
			return Ok("Account activated successfully");
		} 
		catch (UnauthorizedAccessException e) {
			Console.WriteLine(e);
			return NotFound(e.Message);
		} 
		catch (BadHttpRequestException e) {
			Console.WriteLine(e);
			return BadRequest(e.Message);
		} 
		catch (Exception e) {
			Console.WriteLine(e);
			return StatusCode(StatusCodes.Status500InternalServerError, $"Issues activating account: {e.Message}");
		}
	}

	[Authorize]
	[HttpGet("validateToken")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult ValidateToken() {
		return Ok(new { status = "Valid" });
	}
}
