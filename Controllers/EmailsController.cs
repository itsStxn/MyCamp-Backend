using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailsController(IEmailService emailService) : ControllerBase{
	private readonly IEmailService EmailService = emailService;

	[HttpPost("contactUs")]
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
