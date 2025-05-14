using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shared.Endpoints.Results;
using Shared.Exceptions;
using System.Linq.Expressions;

namespace Shared.Endpoints.Validation;

public static class Extensions
{
	public static IEndpointConventionBuilder AddValidation<T>(this IEndpointConventionBuilder builder) =>
		builder.AddEndpointFilterFactory((factory, next) =>
		{
			var validatableType = typeof(T);

			var parameters = factory.MethodInfo.GetParameters();
			var parameterIndex = Array.FindIndex(parameters, x => x.ParameterType == typeof(T));

			Expression<Func<EndpointFilterInvocationContext, T>> accessorExpression;

			if (parameterIndex != -1)
			{
				accessorExpression = context => context.GetArgument<T>(parameterIndex);
			}
			else
			{
				var requestIndex = Array.FindIndex(parameters, x => x.ParameterType.GetProperties().Any(y => y.PropertyType == validatableType));
				if (requestIndex == -1) throw new CadrException("Missing IHttpRequest in request context.");

				var requestType = parameters[requestIndex].ParameterType;

				var requestProperties = requestType.GetProperties();
				var validatablePropertyIndex = Array.FindIndex(requestProperties, x => x.PropertyType == validatableType);
				if (validatablePropertyIndex == -1) throw new CadrException($"Missing {validatableType.Name} property on {requestType.Name}!");

				var contextExpression = Expression.Parameter(typeof(EndpointFilterInvocationContext));
				var indexExpression = Expression.Constant(requestIndex, typeof(int));

				var getArgument = typeof(EndpointFilterInvocationContext)
					.GetMethod(nameof(EndpointFilterInvocationContext.GetArgument))?
					.MakeGenericMethod(requestType)!;

				var callExpression = Expression.Call(contextExpression, getArgument, indexExpression);
				var propertyExpression = Expression.Property(callExpression, requestProperties[validatablePropertyIndex]);

				accessorExpression = Expression.Lambda<Func<EndpointFilterInvocationContext, T>>(propertyExpression, contextExpression);
			}

			var accessor = accessorExpression.Compile();

			return async (context) =>
			{
				var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
				var request = accessor(context);

				var validationResult = await validator.ValidateAsync(request);

				if (validationResult.IsValid) return await next(context);

				var errorDetails = validationResult.Errors
					.GroupBy(x => x.PropertyName)
					.Select(x => new ErrorResultDetail(x.Key, x.Select(y => y.ErrorMessage)));

				return SharedErrors.ValidationError(errorDetails);
			};
		});
}