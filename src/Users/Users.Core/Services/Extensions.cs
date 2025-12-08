using Microsoft.AspNetCore.Http;
using System.Net;

namespace Users.Core.Services;

internal static class Extensions
{
	public static string GetClientIpAddress(this HttpContext httpContext)
	{
		var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
		if (!string.IsNullOrEmpty(clientIp)) return clientIp.Split(',')[0];

		var ipAddress = httpContext.Connection.RemoteIpAddress!;
		return IPAddress.IsLoopback(ipAddress) ? "8.8.8.8" : ipAddress.ToString();
	}
}