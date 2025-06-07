namespace Shared.Settings;

public sealed record PostgreSqlSettings(string ConnectionString) : ISettings
{
	public static string SectionName => "PostgreSql";
}