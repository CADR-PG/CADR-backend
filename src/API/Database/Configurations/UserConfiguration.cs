// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using API.Modules.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Database.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("Users");
		builder.HasKey(u => u.Id);
		builder.Property(u => u.FirstName)
			.IsRequired()
			.HasMaxLength(50);
		builder.Property(u => u.LastName)
			.IsRequired()
			.HasMaxLength(50);
		builder.Property(u => u.Password)
			.IsRequired()
			.HasMaxLength(100);
		builder.Property(u => u.Email)
			.IsRequired()
			.HasMaxLength(320);
		builder.Property(u => u.Phone)
			.IsRequired()
			.HasMaxLength(15);
		builder.Property(u => u.BirthDate)
			.IsRequired();
	}
}