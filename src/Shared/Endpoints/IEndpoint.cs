using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Shared.Endpoints;

public interface IEndpoint
{
	static abstract void Register(IEndpointRouteBuilder endpoints);
}

public interface IHttpRequest;

public interface IHttpRequestHandler<in TRequest> where TRequest : IHttpRequest
{
	Task<IResult> Handle(TRequest request, CancellationToken cancellationToken);
}