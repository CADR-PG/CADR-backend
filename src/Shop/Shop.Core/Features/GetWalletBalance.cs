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
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetWalletBalanceHandler(
	ShopDbContext dbContext
) : IHttpRequestHandler<GetWalletBalance>
{
	public async Task<IResult> Handle(GetWalletBalance request, CancellationToken cancellationToken)
	{
		var wallet = await dbContext.Wallets.FirstOrDefaultAsync(x => x.UserId == request.CurrentUser.Id, cancellationToken);
		var balance = wallet?.Ballance ?? 0;
		return Results.Ok($"{balance / 100m:0.00} PLN");
	}
}