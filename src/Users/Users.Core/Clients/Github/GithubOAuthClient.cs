using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Extensions;
using Shared.Settings;
using Users.Core.Clients.Github.Requests;
using Users.Core.Clients.Github.Responses;
using Users.Core.Settings;

namespace Users.Core.Clients.Github;


internal interface IGithubOAuthClient
{
	Task<GitHubAccessTokenResponse> ExchangeCodeForToken(string code);
}

file sealed class GithubOAuthClient(HttpClient client, IOptions<GithubOAuthClientSettings> options) : IGithubOAuthClient
{
	private GithubOAuthClientSettings Settings { get; } = options.Value;

	public async Task<GitHubAccessTokenResponse> ExchangeCodeForToken(string code)
	{
		var response = await client.PostAsync<GitHubAccessTokenRequest, GitHubAccessTokenResponse>(
			new Uri("/login/oauth/access_token", UriKind.Relative),
			new GitHubAccessTokenRequest
			{
				ClientId = Settings.ClientId,
				ClientSecret = Settings.ClientSecret,
				Code = code,
				RedirectUri = null
			});

		return response;
	}
}

internal static class GithubOAuthClientExtensions
{
	public static void RegisterGithubOAuthClient(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSettingsWithOptions<GithubOAuthClientSettings>(configuration);
		services.AddHttpClient<IGithubOAuthClient, GithubOAuthClient>(client =>
		{
			client.BaseAddress = new Uri("https://github.com", UriKind.Absolute);
			client.DefaultRequestHeaders.Add("Accept", "application/json");
		});
	}
}