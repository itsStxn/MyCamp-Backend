﻿using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class AuthUser {
	[Key]
	public int Id { get; set; }
	public required string Username { get; set; }
	public required string Email { get; set; }
	public required string Password { get; set; }
}
