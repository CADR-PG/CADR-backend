using Microsoft.EntityFrameworkCore;
using Shared.Modules;
using System.Reflection;

namespace Shared.ValueObjects.SnowflakeIds;

public static class Extensions
{
	public static void RegisterSnowflakeIdsConversion<TModule>(this ModelConfigurationBuilder builder) where TModule : IModule
	{
		var openInterface = typeof(ISnowflakeId<>);

		var registerMethod = typeof(Extensions).GetMethod(nameof(RegisterConverter), BindingFlags.NonPublic | BindingFlags.Static)!;

		var types = typeof(TModule).Assembly.GetTypes() // module types
			.Concat(openInterface.Assembly.GetTypes()) // shared value objects
			.Where(t =>
				t is { IsValueType: true, IsInterface: false, IsGenericTypeDefinition: false }
				&& t.GetInterfaces().Any(i =>
					i.IsGenericType &&
					i.GetGenericTypeDefinition() == openInterface));

		foreach (var type in types)
			registerMethod.MakeGenericMethod(type)
				.Invoke(null, [builder]);
	}

	private static void RegisterConverter<T>(
		ModelConfigurationBuilder builder)
		where T : struct, ISnowflakeId<T>
	{
		builder.Properties<T>().HaveConversion<EfCoreSnowflakeIdConverter<T>>();
	}
}