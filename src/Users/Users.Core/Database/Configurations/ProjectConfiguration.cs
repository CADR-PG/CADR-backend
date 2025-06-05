using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Users.Core.Entities;

namespace Users.Core.Database.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
	public void Configure(EntityTypeBuilder<Project> builder)
	{
		builder.HasKey(p => p.Id);
		builder.Property(p => p.Id).ValueGeneratedOnAdd();
		builder.Property(p => p.Name).IsRequired();
		builder.Property(p => p.Description).IsRequired();
		builder.Property(p => p.JsonDocument).IsRequired().HasColumnType("json").HasConversion(
			v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
			v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>()
			);

		builder.Property(p => p.UserId).IsRequired();
	}
}