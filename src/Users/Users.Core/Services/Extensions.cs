using Microsoft.AspNetCore.Http;

namespace Users.Core.Services;

internal static class Extensions
{
	public static string GetClientIpAddress(this HttpContext httpContext)
	{
		var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
		return !string.IsNullOrEmpty(clientIp)
			? clientIp.Split(',')[0]
			: httpContext.Connection.RemoteIpAddress!.ToString();
	}
}