using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailsController(IEmailService emailService) : ControllerBase {
	private readonly IEmailService EmailService = emailService;

/// <summary>
/// Receives a contact us message from a client.
/// The message will be sent to the server owner's email address.
/// </summary>
/// <param name="request">The contact us request containing the email, subject, and message.</param>
/// <returns>A JSON object with a message indicating the result of sending the message.</returns>
/// <response code="200">The message was sent successfully.</response>
/// <response code="500">An internal server error occurred.</response>
	[HttpPost("contactUs")]
	[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
	public IActionResult ReceiveMessage([FromBody] ContactUsRequest request) {
		try {
			string toEmail = "heboivan@gmail.com";
			string subject = request.Subject;
			string body = @$"
				<h1>From: {request.Email}</h1>
				<p>{request.Message}</p>
			";
			EmailService.SendContactUsMail(toEmail, subject, body);
			return Ok(new { message = "Your message has been received!" });
		}
		catch (Exception ex) {
			return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
		}
	}
}
