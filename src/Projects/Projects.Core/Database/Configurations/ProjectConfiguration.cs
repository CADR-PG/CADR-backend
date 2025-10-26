using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Core.Entities;
using Shared.ValueObjects;
using System.Text.Json;

namespace Projects.Core.Database.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
	public void Configure(EntityTypeBuilder<Project> builder)
	{
		builder.HasKey(p => p.Id);
		builder.Property(p => p.Id).ValueGeneratedOnAdd();
		builder.Property(p => p.UserId).HasConversion(
				id => id.Value,
				value => new UserId(value)
			).IsRequired();
		builder.Property(p => p.Name).IsRequired();
		builder.Property(p => p.Description).IsRequired();
		builder.Property(p => p.JsonDocument);
		// builder.HasOne(p => p.User).WithMany(u => u.Projects).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
	}
}