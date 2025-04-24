namespace API.Shared.Endpoints;

internal interface IEndpoint
{
	static abstract void Register(IEndpointRouteBuilder endpoints);
}

internal interface IHttpRequest;

internal interface IHttpRequestHandler<in TRequest> where TRequest : IHttpRequest
{
	Task<IResult> Handle(TRequest request, CancellationToken cancellationToken);
}