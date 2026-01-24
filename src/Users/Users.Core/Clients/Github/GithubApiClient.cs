using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Users.Core.Clients.Github.Responses;

namespace Users.Core.Clients.Github;

internal interface IGithubClient
{
	Task<User> GetUser(string accessToken);
}

file sealed class GithubApiClient(HttpClient client) : IGithubClient
{
	public async Task<User> GetUser(string accessToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("user", UriKind.Relative));
		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

		var response = await client.SendAsync(request);
		response.EnsureSuccessStatusCode();

		var user = await response.Content.ReadFromJsonAsync<User>(
			new JsonSerializerOptions(JsonSerializerDefaults.Web)
		);

		return user!;
	}
}

internal static class GithubClientExtensions
{
	public static void RegisterGithubClient(this IServiceCollection services)
	{
		services.AddHttpClient<IGithubClient, GithubApiClient>(client =>
		{
			client.BaseAddress = new Uri("https://api.github.com/", UriKind.Absolute);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("cadr.studio");
		});
	}
}