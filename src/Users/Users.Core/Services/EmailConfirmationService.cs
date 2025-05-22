using Shared.Services;
using Users.Core.Entities;

namespace Users.Core.Services;

internal sealed class EmailConfirmationService(IMailingService mailingService)
{
	public async Task SendEmailConfirmation(User user)
	{
		await mailingService.SendAsync(user.FullName, user.Email, "Email confirmation", $"Your confirmation code: {user.EmailConfirmation.Code}. Verify your mail: <TODO>");
	}
}