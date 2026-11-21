using System.ComponentModel.DataAnnotations;

namespace Shared.Settings;

public sealed record SnowflakeIdSettings : ISettings
{
	public static string SectionName => "Shared:SnowflakeId";

	/// <summary>Unique generator (worker) identifier, 0-1023. Must differ between running instances.</summary>
	[Range(0, 1023)]
	public required int GeneratorId { get; init; }

	/// <summary>Custom epoch used as timestamp origin. Must never change once IDs were generated.</summary>
	public DateTimeOffset Epoch { get; init; } = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
