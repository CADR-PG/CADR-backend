using Microsoft.Extensions.Hosting;
using Shared.Exceptions;
using Shared.Settings;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

namespace Shared.Messaging;

public static class Extensions
{
	/// <summary>
	/// Configures Wolverine with Azure Service Bus as the transport and the transactional outbox / durable inbox
	/// stored in PostgreSQL. Modules add their own routing (publishing rules, subscriptions, handler discovery)
	/// with <c>services.ConfigureWolverine(...)</c>.
	/// </summary>
	/// <param name="builder">Host builder.</param>
	/// <param name="serviceName">
	/// Name of the running service. Every service gets its own message store schema (<c>wolverine_{serviceName}</c>),
	/// because Wolverine nodes sharing a message store are expected to run the same handlers and endpoints.
	/// </param>
	/// <param name="handlesMessages">
	/// <c>false</c> for hosts that only publish messages through the outbox (e.g. the API),
	/// <c>true</c> for hosts that listen to subscriptions and run message handlers (the message broker).
	/// </param>
	public static void AddMessaging(this IHostApplicationBuilder builder, string serviceName, bool handlesMessages)
	{
		var postgreSqlSettings = builder.Configuration.GetSettings<PostgreSqlSettings>();
		var messagingSettings = builder.Configuration.GetSettings<MessagingSettings>();

		if (string.IsNullOrWhiteSpace(messagingSettings.AzureServiceBusConnectionString))
			throw new CadrException($"`{MessagingSettings.SectionName}:{nameof(MessagingSettings.AzureServiceBusConnectionString)}` is required.");

		builder.UseWolverine(options =>
		{
			options.ServiceName = serviceName;

			options.PersistMessagesWithPostgresql(postgreSqlSettings.ConnectionString, $"wolverine_{serviceName.Replace('-', '_')}");
			options.UseEntityFrameworkCoreTransactions();

			options.Policies.UseDurableOutboxOnAllSendingEndpoints();
			options.Policies.UseDurableInboxOnAllListeners();

			var azureServiceBus = messagingSettings.UsesAzureServiceBusEmulator
				? options.UseAzureServiceBusEmulator(
					messagingSettings.AzureServiceBusConnectionString,
					messagingSettings.AzureServiceBusManagementConnectionString
					?? throw new CadrException($"`{MessagingSettings.SectionName}:{nameof(MessagingSettings.AzureServiceBusManagementConnectionString)}` is required by the Azure Service Bus emulator."))
				: options.UseAzureServiceBus(messagingSettings.AzureServiceBusConnectionString);

			if (messagingSettings.AutoProvision)
				azureServiceBus.AutoProvision();

			// Publishers do not need per-node response/retry queues. Consumers keep Wolverine's retry queue:
			// without it, retries of messages received from a topic subscription would be re-published
			// to the topic and fanned out to every other subscription.
			azureServiceBus.SystemQueuesAreEnabled(handlesMessages);

			if (handlesMessages)
				options.ApplyMessageHandlingPolicies();
		});
	}

	/// <summary>
	/// Policies applied to every message handler of a host that handles messages.
	/// </summary>
	public static void ApplyMessageHandlingPolicies(this WolverineOptions options)
	{
		options.Policies.AutoApplyTransactions();

		options.Policies.OnAnyException()
			.RetryWithCooldown(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2))
			.Then.ScheduleRetry(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
			.Then.MoveToErrorQueue();
	}
}