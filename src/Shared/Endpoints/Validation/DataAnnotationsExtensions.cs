using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shared.Endpoints.Results;
using Shared.Exceptions;

namespace Shared.Endpoints.Validation;

/// <summary>
/// Validates the request payload using <see cref="System.ComponentModel.DataAnnotations"/> attributes
/// (e.g. <c>[Required]</c>, <c>[MaxLength]</c>) instead of a FluentValidation validator.
/// </summary>
public static class DataAnnotationsExtensions
{
	public static RouteHandlerBuilder AddDataAnnotationsValidation<T>(this RouteHandlerBuilder builder)
	{
		return builder.AddEndpointFilterFactory((factory, next) =>
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
				if (requestIndex == -1)
				{
					throw new CadrException("Missing IHttpRequest in request context.");
				}

				var requestType = parameters[requestIndex].ParameterType;

				var requestProperties = requestType.GetProperties();
				var validatablePropertyIndex = Array.FindIndex(requestProperties, x => x.PropertyType == validatableType);
				if (validatablePropertyIndex == -1)
				{
					throw new CadrException($"Missing {validatableType.Name} property on {requestType.Name}!");
				}

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
				var request = accessor(context);

				if (request is null)
				{
					return SharedErrors.ValidationError([new ErrorResultDetail(typeof(T).Name, ["Request body is required."])]);
				}

				var validationResults = new List<ValidationResult>();
				var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

				if (isValid)
				{
					return await next(context);
				}

				var errorDetails = validationResults
					.GroupBy(x => x.MemberNames.FirstOrDefault())
					.Select(x => new ErrorResultDetail(x.Key ?? typeof(T).Name, x.Select(y => y.ErrorMessage ?? string.Empty)));

				return SharedErrors.ValidationError(errorDetails);
			};
		});
	}
}
