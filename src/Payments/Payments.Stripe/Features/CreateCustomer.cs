using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Payments.Stripe.Database;
using Payments.Stripe.Entities;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Stripe;

namespace Payments.Stripe.Features;

internal sealed record CreateCustomer(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class CreateCustomerEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateCustomer, CreateCustomerHandler>("create-customer")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError")
		.ProducesError(404, "`NotFoundError`");
}

internal sealed class CreateCustomerHandler(PaymentsStripeDbContext dbContext, IStripeClient stripeClient) : IHttpRequestHandler<CreateCustomer>
{
	public async Task<IResult> Handle(CreateCustomer request, CancellationToken cancellationToken)
	{
		var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.CurrentUser.Id, cancellationToken);
		if (user == null)
			return Results.NotFound();
		var options = new CustomerCreateOptions() { Email = user.Email, Name = user.FullName };
		var customerService = new CustomerService(stripeClient);
		var customer = await customerService.CreateAsync(options, cancellationToken: cancellationToken);
		var stripeCustomer = new StripeCutomer() { UserId = request.CurrentUser.Id, StripeCustomerId = customer.Id, };
		await dbContext.StripeCustomers.AddAsync(stripeCustomer, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.Ok(customer);
	}
}