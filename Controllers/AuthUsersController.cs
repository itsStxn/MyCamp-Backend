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
	
/// <summary>
/// Registers a new user in the system. 
/// If a user with the same username or email does not already exist, inserts the user and sends a confirmation email.
/// If the user exists but is not active, resumes the user and sends a confirmation email.
/// Throws an exception if the user already exists and is active.
/// </summary>
/// <param name="user">The User object containing the registration details.</param>
/// <returns>True if the user was successfully registered or resumed, otherwise false.</returns>
/// <response code="201">User added successfully</response>
/// <response code="400">User already exists and is active</response>
/// <response code="404">User not found</response>
/// <response code="409">User already exists but is not active</response>
/// <response code="500">Unexpected error</response>
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

/// <summary>
/// Authenticates a user by searching for them in the database and verifying the password.
/// If the user is found and the password is correct, returns an AuthTicket containing the JWT and the User.
/// If the user is not found, or the password is incorrect, returns null.
/// If the user is not active, throws an UnauthorizedAccessException.
/// </summary>
/// <param name="user">The AuthUser object containing the username and password to authenticate.</param>
/// <returns>An AuthTicket if the user is authenticated, otherwise null.</returns>
/// <response code="200">User authenticated successfully</response>
/// <response code="401">Wrong password</response>
/// <response code="404">User not found</response>
/// <response code="500">Unexpected error</response>
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

/// <summary>
/// Sends a password reset link to the user with the given email or username.
/// If a user with the given email or username does not exist, throws a KeyNotFoundException.
/// </summary>
/// <param name="cred">The MailCredentials object containing the user email or username.</param>
/// <returns>The email address if the link was sent, otherwise null.</returns>
/// <response code="200">Link sent successfully</response>
/// <response code="400">Failed to mail credentials setter</response>
/// <response code="404">User not found</response>
/// <response code="500">Unexpected error</response>
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

/// <summary>
/// Changes the password of the authenticated user using a valid reset password token.
/// If the password change is successful, returns an HTTP 200 OK response.
/// If the token is invalid or the password format is incorrect, returns an HTTP 400 Bad Request response.
/// If any other error occurs, returns an HTTP 500 Internal Server Error response.
/// </summary>
/// <param name="req">The ChangePassword object containing the new password.</param>
/// <returns>A response indicating the success or failure of the password change operation.</returns>
/// <response code="200">Password changed successfully</response>
/// <response code="400">Failed to change password or delete token</response>
/// <response code="500">Issues changing password</response>
	[Authorize]
	[HttpPut("changePassword")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult ChangePassword([FromBody] ChangePassword req) {
		try {
			var token = ReqHelper.GetToken();
			int userID = int.Parse(RequestHelper.GetNameIdentifier(User));
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

/// <summary>
/// Activates a user given a valid activation token.
/// The user is identified by the given user ID.
/// The user is then activated in the database.
/// The token is then deleted to prevent further use.
/// </summary>
/// <returns>A response indicating the success or failure of the activation operation.</returns>
/// <response code="200">Account activated successfully</response>
/// <response code="400">Failed to activate account or delete token</response>
/// <response code="404">User not found</response>
/// <response code="500">Unexpected error</response>
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

/// <summary>
/// Validates the current token.
/// </summary>
/// <returns>A response with a field status with the value "Valid" if the token is valid, otherwise a 401 Unauthorized response.</returns>
/// <response code="200">Token is valid</response>
/// <response code="401">Token is invalid</response>
	[Authorize]
	[HttpGet("validateToken")]
	[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
	public IActionResult ValidateToken() {
		return Ok(new { status = "Valid" });
	}
}
