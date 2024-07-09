using Server.Interfaces;
using Server.Utils;

namespace Server.Services;

public class EmailService(ISecretKeyService secretService) : IEmailService {
	private readonly EmailHelper EmailHelper = new(secretService);

	public void SendActivationMail(string email, string token) {
		string subject = "Activate your MyCamp account";
		string body = EmailHelper.ActivateAccountBody(token);
		EmailHelper.SendEmail(email, subject, body);
	}
	public void SendResetPasswordMail(string email, string token) {
		string subject = "Reset your MyCamp password";
		string body = EmailHelper.ForgotPasswordBody(token);
		EmailHelper.SendEmail(email, subject, body);
	}
	public void SendContactUsMail(string toEmail, string subject, string message) {
		EmailHelper.SendEmail(toEmail, subject, message);
	}

}
