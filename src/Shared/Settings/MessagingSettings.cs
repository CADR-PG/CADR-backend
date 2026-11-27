namespace Shared.Settings;

public sealed record MessagingSettings : ISettings
{
	public static string SectionName => "Shared:Messaging";

	/// <summary>Azure Service Bus (or Azure Service Bus emulator) connection string.</summary>
	public required string AzureServiceBusConnectionString { get; init; }

	/// <summary>
	/// Management (HTTP) connection string - required only by the Azure Service Bus emulator,
	/// which exposes messaging (AMQP) and management APIs on different ports.
	/// </summary>
	public string? AzureServiceBusManagementConnectionString { get; init; }

	/// <summary>
	/// Create missing topics, subscriptions and queues on startup. Requires a `Manage` claim on the Service Bus credentials.
	/// </summary>
	public bool AutoProvision { get; init; } = true;

	public bool UsesAzureServiceBusEmulator
		=> AzureServiceBusConnectionString.Contains("UseDevelopmentEmulator=true", StringComparison.OrdinalIgnoreCase);
}