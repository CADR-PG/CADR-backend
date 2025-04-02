namespace API.Shared.Endpoints;

public static class Extensions
{
	public static RouteHandlerBuilder MapGet<TRequest, THandler>(this IEndpointRouteBuilder endpoints, string template)
		where TRequest : IHttpRequest
		where THandler : IHttpRequestHandler<TRequest> =>
		endpoints.MapGet(template, async (
					THandler handler,
					[AsParameters] TRequest query,
					CancellationToken cancellationToken) =>
				await handler.Handle(query, cancellationToken))
			.WithSummary(typeof(TRequest).Name);

	public static RouteHandlerBuilder MapPost<TRequest, THandler>(this IEndpointRouteBuilder endpoints, string template)
		where TRequest : IHttpRequest
		where THandler : IHttpRequestHandler<TRequest> =>
		endpoints.MapPost(template, async (
					THandler handler,
					[AsParameters] TRequest query,
					CancellationToken cancellationToken) =>
				await handler.Handle(query, cancellationToken))
			.WithSummary(typeof(TRequest).Name);

	public static RouteHandlerBuilder MapPut<TRequest, THandler>(this IEndpointRouteBuilder endpoints, string template)
		where TRequest : IHttpRequest
		where THandler : IHttpRequestHandler<TRequest> =>
		endpoints.MapPut(template, async (
					THandler handler,
					[AsParameters] TRequest query,
					CancellationToken cancellationToken) =>
				await handler.Handle(query, cancellationToken))
			.WithSummary(typeof(TRequest).Name);

	public static RouteHandlerBuilder MapPath<TRequest, THandler>(this IEndpointRouteBuilder endpoints, string template)
		where TRequest : IHttpRequest
		where THandler : IHttpRequestHandler<TRequest> =>
		endpoints.MapPatch(template, async (
					THandler handler,
					[AsParameters] TRequest query,
					CancellationToken cancellationToken) =>
				await handler.Handle(query, cancellationToken))
			.WithSummary(typeof(TRequest).Name);


	public static RouteHandlerBuilder MapDelete<TRequest, THandler>(this IEndpointRouteBuilder endpoints,
		string template)
		where TRequest : IHttpRequest
		where THandler : IHttpRequestHandler<TRequest> =>
		endpoints.MapDelete(template, async (
					THandler handler,
					[AsParameters] TRequest query,
					CancellationToken cancellationToken) =>
				await handler.Handle(query, cancellationToken))
			.WithSummary(typeof(TRequest).Name);

	public static IEndpointRouteBuilder Map<TEndpoint>(this IEndpointRouteBuilder endpoints)
		where TEndpoint : IEndpoint
	{
		TEndpoint.Register(endpoints);
		return endpoints;
	}
}
