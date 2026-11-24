using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Payments.Stripe.Database;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Stripe;

namespace Payments.Stripe.Features;

internal sealed record Pay(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class PayEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<Pay, PayHandler>("pay")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError")
		.ProducesError(404, "`NotFoundError`");
}

internal sealed class PayHandler(PaymentsStripeDbContext dbContext) : IHttpRequestHandler<Pay>
{
	public async Task<IResult> Handle(Pay request, CancellationToken cancellationToken)
	{
		var stripeCustomer = await dbContext.StripeCustomers.FirstOrDefaultAsync(u => u.UserId == request.CurrentUser.Id, cancellationToken);
		if (stripeCustomer == null)
			return Results.NotFound();
		return Results.Ok(stripeCustomer);
	}
}