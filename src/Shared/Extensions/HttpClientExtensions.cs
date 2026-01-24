using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Shared.Extensions;

public static class HttpClientExtensions
{
	public static async Task<TResponse> PostAsync<TRequest, TResponse>(this HttpClient client, Uri requestUri, TRequest requestBody)
	{
		using StringContent jsonContent = new(
			JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web),
			Encoding.UTF8,
			"application/json");

		using var response = await client.PostAsync(
			requestUri,
			jsonContent).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		var responseBody = await response.Content.ReadFromJsonAsync<TResponse>();
		return responseBody!;
	}
}