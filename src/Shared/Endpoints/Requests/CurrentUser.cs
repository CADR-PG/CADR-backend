using Microsoft.AspNetCore.Http;
using Shared.ValueObjects;

namespace Shared.Endpoints.Requests;

public record CurrentUser(UserId Id)
{
	public static ValueTask<CurrentUser?> BindAsync(HttpContext context)
	{
		var userIdClaim = context.User.Claims.FirstOrDefault(x => x.Type == "sub");

		var currentUser = userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var id)
			? new CurrentUser(new UserId(id))
			: null;

		return ValueTask.FromResult(currentUser);
	}
}