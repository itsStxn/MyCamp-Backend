using Server.Interfaces;
using Server.Utils;

namespace Server.Services;

public class EmailService(ISecretKeyService secretService) : IEmailService {
	private readonly EmailHelper Mail = new(secretService);

/// <summary>
/// Sends an activation email to the given email address.
/// The email will contain a link to activate the account.
/// </summary>
/// <param name="toEmail">The email address to send the email to.</param>
/// <param name="token">The activation token to include in the link.</param>
	public void SendActivationMail(string toEmail, string token) {
		string subject = "Activate your MyCamp account";
		string body = EmailHelper.ActivateAccountBody(token);
		Mail.SendEmail(toEmail, subject, body);
	}
/// <summary>
/// Sends a password reset email to the specified email address.
/// The email will contain a link to reset the password.
/// </summary>
/// <param name="toEmail">The email address to send the email to.</param>
/// <param name="token">The password reset token to include in the link.</param>
	public void SendResetPasswordMail(string toEmail, string token) {
		string subject = "Reset your MyCamp password";
		string body = EmailHelper.ForgotPasswordBody(token);
		Mail.SendEmail(toEmail, subject, body);
	}
/// <summary>
/// Sends a contact us email to the specified email address.
/// The email will contain the specified subject and body.
/// </summary>
/// <param name="toEmail">The email address to send the email to.</param>
/// <param name="subject">The subject of the email.</param>
/// <param name="body">The body content of the email, which can include HTML.</param>
	public void SendContactUsMail(string toEmail, string subject, string body) {
		Mail.SendEmail(toEmail, subject, body);
	}
}
