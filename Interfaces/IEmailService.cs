namespace Server.Interfaces;

public interface IEmailService {
	void SendActivationMail(string toEmail, string token);
	void SendResetPasswordMail(string toEmail, string token);
	void SendContactUsMail(string toEmail, string subject, string body);
}
