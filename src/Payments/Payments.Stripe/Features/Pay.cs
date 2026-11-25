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
using Stripe.Checkout;

namespace Payments.Stripe.Features;

internal sealed record Pay([FromBody] Pay.Data Body, CurrentUser CurrentUser) : IHttpRequest
{
	internal record Data(long Amount, string Currency);
}

internal sealed class PayEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<Pay, PayHandler>("pay")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError")
		.ProducesError(404, "`NotFoundError`");
}

internal sealed class PayHandler(PaymentsStripeDbContext dbContext, IStripeClient stripeClient) : IHttpRequestHandler<Pay>
{
	public async Task<IResult> Handle(Pay request, CancellationToken cancellationToken)
	{
		var stripeCustomer = await dbContext.StripeCustomers.FirstOrDefaultAsync(u => u.UserId == request.CurrentUser.Id, cancellationToken);
		if (stripeCustomer == null)
			return Results.NotFound();

		var options = new SessionCreateOptions
		{
			SuccessUrl = "http://localhost:8080/payments-stripe/success?session_id={CHECKOUT_SESSION_ID}",
			CancelUrl = "http://localhost:8080/payments-stripe/cancel",
			LineItems = new List<SessionLineItemOptions>
			{
				new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						Currency = request.Body.Currency,
						UnitAmount = request.Body.Amount,
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = "Test product", // docelowo tytuł gry
						},
					},
					Quantity = 1,
				},
			},
			Mode = "payment",
		};
		var service = new SessionService(stripeClient);
		Session session = await service.CreateAsync(options, cancellationToken: cancellationToken);

		return Results.Ok(session);
	}
}