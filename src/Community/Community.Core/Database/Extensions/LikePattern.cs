namespace Community.Core.Database.Extensions;

internal static class LikePattern
{
	public const string EscapeCharacter = "\\";

	public const int MaxPhraseLength = 100;

	public static string Escape(string value) => value
		.Replace("\\", "\\\\", StringComparison.Ordinal)
		.Replace("%", "\\%", StringComparison.Ordinal)
		.Replace("_", "\\_", StringComparison.Ordinal);

	public static string Contains(string phrase)
	{
		var trimmed = phrase.Trim();
		if (trimmed.Length > MaxPhraseLength) trimmed = trimmed[..MaxPhraseLength];

		return $"%{Escape(trimmed)}%";
	}
}