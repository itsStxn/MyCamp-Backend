using Server.Interfaces;
using System.Net.Mail;
using System.Net;

namespace Server.Utils;

public class EmailHelper(ISecretKeyService secretService) {
	private readonly ISecretKeyService SecretKeyService = secretService;

/// <summary>
/// Sends an email using the specified recipient email, subject, and body.
/// </summary>
/// <param name="toEmail">The email address of the recipient.</param>
/// <param name="subject">The subject of the email.</param>
/// <param name="body">The body content of the email, which can include HTML.</param>
	public void SendEmail(string toEmail, string subject, string body) {
		var fromEmail = "heboivan@gmail.com";
		var fromPassword = SecretKeyService.GetGmailAppKey();

		using var client = new SmtpClient("smtp.gmail.com", 587);
		client.EnableSsl = true;
		client.DeliveryMethod = SmtpDeliveryMethod.Network;
		client.UseDefaultCredentials = false;
		client.Credentials = new NetworkCredential(fromEmail, fromPassword);

		var mailMessage = new MailMessage {
			From = new MailAddress(fromEmail)
		};
		mailMessage.To.Add(toEmail);
		mailMessage.Subject = subject;
		mailMessage.Body = body;
		mailMessage.IsBodyHtml = true;

		client.Send(mailMessage);
	}
/// <summary>
/// Generates the HTML body content for an activation email.
/// The message will include a link to activate the account.
/// </summary>
/// <param name="token">The activation token to include in the link.</param>
	public static string ActivateAccountBody(string token) {
		string link = $"http://localhost:3000/activateAccount?token={token}";
		return @$"
		<h1>Hello, Camper!</h1><br>
		Thank you for signing up with MyCamp.<br>
		Please click on the link below to activate your account.<br>
		<a href='{link}'>Click here to activate your account</a>";
	}
/// <summary>
/// Generates the HTML body content for a forgot password email.
/// The message will include a link to reset the password.
/// </summary>
/// <param name="token">The password reset token to include in the link.</param>
	public static string ForgotPasswordBody(string token) {
		string link = $"http://localhost:3000/forgotPassword?token={token}";
		return @$"
		<h1>Hello, Camper!</h1>!<br>
		Please click on the link below to reset your password.<br>
		<a href='{link}'>Click here to reset your password</a>";
	}
}
