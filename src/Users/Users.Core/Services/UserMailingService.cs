using Shared.Services;
using Users.Core.Entities;

namespace Users.Core.Services;

internal sealed class UserMailingService(IMailingService mailingService)
{
	public async Task SendEmailConfirmation(User user)
	{
		await mailingService.SendAsync(user.FullName, user.Email, "Email confirmation", $"Your confirmation code: {user.EmailConfirmation.Code}. Verify your mail: <TODO>");
	}

	public async Task SendResetPassword(User user)
	{
		await mailingService.SendAsync(user.FullName, user.Email, "Reset password", $"Your reset password code: {user.EmailConfirmation.Code}. Token is valid only for 12h. Or click: <TODO>");
	}
}