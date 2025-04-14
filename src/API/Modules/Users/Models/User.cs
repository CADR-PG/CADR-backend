﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Modules.Users.Models;

internal class User
{
	public Guid Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string PasswordHash { get; set; }
	public required string Email { get; set; }
	public required string PhoneNumber { get; set; }
}
