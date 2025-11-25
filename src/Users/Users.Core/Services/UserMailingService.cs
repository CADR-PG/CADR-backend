using Shared.Services;
using Users.Core.Entities;

namespace Users.Core.Services;

// TODO: move to event handlers
internal sealed class UserMailingService(IMailingService mailingService)
{
	public async Task SendUserCreatedEmailConfirmation(User user)
	{
		await mailingService.SendAsync(user.FullName, user.Email, "Confirm account email", $"Your confirmation code: {user.EmailConfirmation.Code}. Verify your mail: <TODO>");
	}

	public async Task SendChangeEmailConfirmation(User user)
	{
		await mailingService.SendAsync(user.FullName, user.Email, "Email confirmation", $"Your confirmation code: {user.EmailConfirmation.Code}. Verify your mail: <TODO>");
	}

	public async Task SendResetPassword(User user)
	{
		await mailingService.SendAsync(user.FullName, user.Email, "Reset password", $"Your reset password code: {user.EmailConfirmation.Code}. Token is valid only for 12h. Or click: <TODO>");
	}

	public async Task ResendEmailConfirmation(User user)
	{
		await mailingService.SendAsync(user.FullName, user.Email, "Email verification", $"Your confirmation code: {user.EmailConfirmation.Code}. Token is valid only for 12h. Or click: <TODO>");
	}
}