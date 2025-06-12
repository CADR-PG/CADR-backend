using Shared.Services;
using Users.Core.Entities;

namespace Users.Core.Services;

#pragma warning disable CS9113 // Parameter is unread.
internal sealed class EmailConfirmationService(IMailingService mailingService)
#pragma warning restore CS9113 // Parameter is unread.
{
	// ReSharper disable once MemberCanBeMadeStatic.Global
#pragma warning disable CA1822
	public Task SendEmailConfirmation(User user)
#pragma warning restore CA1822
	{
		return Task.CompletedTask;
	}
}