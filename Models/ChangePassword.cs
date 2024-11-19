using System.Text.Json.Serialization;

namespace Server.Models;

public class ChangePassword {
	public required string NewPassword { get; set; }
}
