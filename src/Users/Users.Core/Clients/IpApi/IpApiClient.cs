using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Users.Core.Clients.IpApi.Responses;

namespace Users.Core.Clients.IpApi;

internal interface IIpApiClient
{
	Task<IpAddressLocationReadModel> GetIpAddressGeolocationData(string ipAddress);
}

file sealed class IpApiClient(HttpClient client) : IIpApiClient
{
	public async Task<IpAddressLocationReadModel> GetIpAddressGeolocationData(string ipAddress)
	{
		var response = await client.GetFromJsonAsync<IpAddressLocationReadModel>(
			new Uri($"/{ipAddress}/json", UriKind.Relative),
			new JsonSerializerOptions(JsonSerializerDefaults.Web));

		return response!;
	}
}

internal static class IpApiClientExtensions
{
	public static void RegisterIpApiClient(this IServiceCollection services)
	{
		services.AddHttpClient<IIpApiClient, IpApiClient>(client =>
		{
			client.BaseAddress = new Uri("https://ipapi.co/", UriKind.Absolute);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("api.cadr.studio");
		});
	}
}