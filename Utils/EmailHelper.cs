using System.Net.Mail;
using System.Net;
using Server.Interfaces;

namespace Server.Utils;

public class EmailHelper(ISecretKeyService secretService) {
	private readonly ISecretKeyService SecretKeyService = secretService;

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
	public string ActivateAccountBody(string token) {
		string link = $"http://localhost:3000/activateAccount?token={token}";
		return @$"
		<h1>Hello, Camper!</h1><br>
		Thank you for signing up with MyCamp.<br>
		Please click on the link below to activate your account.<br>
		<a href='{link}'>Click here to activate your account</a>";
	}
	public string ForgotPasswordBody(string token) {
		string link = $"http://localhost:3000/forgotPassword?token={token}";
		return @$"
		<h1>Hello, Camper!</h1>!<br>
		Please click on the link below to reset your password.<br>
		<a href='{link}'>Click here to reset your password</a>";
	}
}
