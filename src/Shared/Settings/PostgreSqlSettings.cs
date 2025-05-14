namespace Shared.Settings;

public record PostgreSqlSettings(string ConnectionString) : ISettings
{
	public static string SectionName => "PostgreSql";
}