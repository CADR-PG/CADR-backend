using Microsoft.AspNetCore.Http;
using System.Net;

namespace Users.Core.Services;

internal static class Extensions
{
	public static string GetClientIpAddress(this HttpContext httpContext)
	{
		var ipAddress = httpContext.Connection.RemoteIpAddress!;
		return IPAddress.IsLoopback(ipAddress) ? "8.8.8.8" : ipAddress.ToString();
	}
}