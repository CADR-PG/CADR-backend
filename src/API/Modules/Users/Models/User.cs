// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace API.Modules.Users.Models
{
	internal class User
	{
		public required int Id { get; set; }
		public required string FirstName { get; set; }
		public required string LastName { get; set; }
		public required string Password { get; set; }
		public required string Email { get; set; }
		public required string Phone { get; set; }
		public required int Age { get; set; }
	}
}
