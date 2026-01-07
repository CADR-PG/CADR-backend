namespace Users.Core.ReadModels;

public class GoogleTokensReadModel
{
	public string? IdToken { get; set; }
	public string? AccessToken { get; set; }
	public string? RefreshToken { get; set; }
}