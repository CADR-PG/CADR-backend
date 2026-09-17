using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Features;

internal sealed record GetWalletBalance(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class GetWalletBalanceEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<GetWalletBalance, GetWalletBalanceHandler>("wallet-balance")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`GameVersionNotFound`");
}

internal sealed class GetWalletBalanceHandler(
	ShopDbContext dbContext
) : IHttpRequestHandler<GetWalletBalance>
{
	public async Task<IResult> Handle(GetWalletBalance request, CancellationToken cancellationToken)
	{
		var balance = await dbContext.Wallets.FirstOrDefaultAsync(x => x.UserId == request.CurrentUser.Id, cancellationToken);
		return Results.Ok((balance!.Ballance / 100) + "PLN");
	}
}