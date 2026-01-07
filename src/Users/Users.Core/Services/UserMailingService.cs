using Shared.Services;
using Users.Core.Entities;

namespace Users.Core.Services;

// TODO: move to event handlers
internal sealed class UserMailingService(IMailingService mailingService)
{
	public async Task SendUserCreatedEmailConfirmation(User user)
	{
		const string subject = "Confirm account email";
		var content = GetMailTemplate(subject, tag: null, $"""
		                                                   <tr>
		                                                     <td style="font-size:15px;line-height:1.6;color:#d1d5db;padding-bottom:24px;">
		                                                       Your confirmation code:
		                                                       <strong style="color:#ffffff;">{user.EmailConfirmation.Code}</strong>
		                                                       <br/>
		                                                    Code is valid for 12h.
		                                                     </td>
		                                                   </tr>

		                                                   <tr>
		                                                     <td style="border-top:1px solid #262b36;padding-top:16px;">
		                                                       <span style="font-size:13px;color:#9ca3af;">
		                                                         If this wasn’t you, ignore this e-mail.
		                                                       </span>
		                                                     </td>
		                                                   </tr>
		                                                   """);

		await mailingService.SendHtmlAsync(user.FullName, user.Email, subject, content);
	}

	public async Task SendChangeEmailConfirmation(User user)
	{
		const string subject = "Confirm new email address";
		var content = GetMailTemplate(subject, tag: null, $"""
		                                                   <tr>
		                                                     <td style="font-size:15px;line-height:1.6;color:#d1d5db;padding-bottom:24px;">
		                                                       Your confirmation code:
		                                                       <strong style="color:#ffffff;">{user.EmailConfirmation.Code}</strong>
		                                                       <br/>
		                                                       Code is valid for 12h.
		                                                     </td>
		                                                   </tr>

		                                                   <tr>
		                                                     <td style="border-top:1px solid #262b36;padding-top:16px;">
		                                                       <span style="font-size:13px;color:#9ca3af;">
		                                                         If this wasn’t you, ignore this e-mail.
		                                                       </span>
		                                                     </td>
		                                                   </tr>
		                                                   """);

		await mailingService.SendHtmlAsync(user.FullName, user.Email, subject, content);
	}

	public async Task SendResetPassword(User user)
	{
		const string subject = "Reset password";
		var content = GetMailTemplate(subject, tag: null, $"""
		                                                   <tr>
		                                                     <td style="font-size:15px;line-height:1.6;color:#d1d5db;padding-bottom:24px;">
		                                                       Your reset password code:
		                                                       <strong style="color:#ffffff;">{user.PasswordResetToken}</strong>
		                                                       <br/>
		                                                       Token is valid only for 1h.
		                                                     </td>
		                                                   </tr>

		                                                   <tr>
		                                                     <td style="border-top:1px solid #262b36;padding-top:16px;">
		                                                       <span style="font-size:13px;color:#9ca3af;">
		                                                         If this wasn’t you, ignore this e-mail.
		                                                       </span>
		                                                     </td>
		                                                   </tr>
		                                                   """);

		await mailingService.SendHtmlAsync(user.FullName, user.Email, subject, content);
	}

	public async Task ResendEmailConfirmation(User user)
	{
		const string subject = "Reset password";
		var content = GetMailTemplate(subject, tag: null, $"""
		                                                   <tr>
		                                                     <td style="font-size:15px;line-height:1.6;color:#d1d5db;padding-bottom:24px;">
		                                                       Your confirmation code:
		                                                       <strong style="color:#ffffff;">{user.EmailConfirmation.Code}</strong>
		                                                       <br/>
		                                                       Code is valid for 12h.
		                                                     </td>
		                                                   </tr>

		                                                   <tr>
		                                                     <td style="border-top:1px solid #262b36;padding-top:16px;">
		                                                       <span style="font-size:13px;color:#9ca3af;">
		                                                         If this wasn’t you, ignore this e-mail.
		                                                       </span>
		                                                     </td>
		                                                   </tr>
		                                                   """);

		await mailingService.SendHtmlAsync(user.FullName, user.Email, subject, content);
	}

	public async Task SendUserLoggedIn(User user)
	{
		var lastLocations = user.UserLocationLogs.OrderByDescending(x => x.OccuredAt).Take(2).ToList();

		var message = lastLocations.Count == 0 || lastLocations.Count == 2 &&
			lastLocations[0].City == lastLocations[1].City && lastLocations[0].Country == lastLocations[1].Country
				? GetMailTemplate("Login activity notification", tag: null, $"""
				                                                             <tr>
				                                                               <td style="font-size:15px;line-height:1.6;color:#d1d5db;padding-bottom:24px;">
				                                                                 You have logged in again from
				                                                                 <strong style="color:#ffffff;">{lastLocations[0].City}, {lastLocations[0].Country}</strong>
				                                                                 using IP address
				                                                                 <strong style="color:#ffffff;">{lastLocations[0].IpAddress}</strong>.
				                                                               </td>
				                                                             </tr>

				                                                             <tr>
				                                                               <td style="border-top:1px solid #262b36;padding-top:16px;">
				                                                                 <span style="font-size:13px;color:#9ca3af;">
				                                                                   If this wasn’t you, we recommend changing your password immediately.
				                                                                 </span>
				                                                               </td>
				                                                             </tr>
				                                                             """)
				: GetMailTemplate("Login activity notification", "New location", $"""
					 <tr>
					   <td style="font-size:15px;line-height:1.6;color:#d1d5db;padding-bottom:24px;">
					     You logged into your account from a new location
					     <strong style="color:#ffffff;">{lastLocations[0].City}, {lastLocations[0].Country}</strong>
					     using IP address
					     <strong style="color:#ffffff;">{lastLocations[0].IpAddress}</strong>.
					     {(lastLocations.Count == 1 ? string.Empty : $"""
						                                                 <br /><br />
						                                                 The straight-line distance from your previous login location is
						                                                 <strong style="color:#ffffff;">{UserLocationLog.CalculateDistanceKm(lastLocations[0], lastLocations[1]):F2}km</strong>.
						                                                 """)}
					    </td>
					 </tr>

					 <tr>
					   <td style="border-top:1px solid #262b36;padding-top:16px;">
					     <span style="font-size:13px;color:#9ca3af;">
					       If you do not recognize this activity, please secure your account by changing your password.
					     </span>
					   </td>
					 </tr>
					 """);
		await mailingService.SendHtmlAsync(user.FullName, user.Email, "Login activity notification", message);
	}

	private static string GetMailTemplate(string title, string? tag, string content)
	{
		return $"""

		        <!DOCTYPE html>

		        <html lang="en">
		        <head>
		          <meta charset="UTF-8" />
		          <title>{title}</title>
		        </head>
		        <body style="margin:0;padding:0;background-color:#0f1115;font-family:Arial,Helvetica,sans-serif;color:#e5e7eb;">
		          <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0f1115;padding:40px 0;">
		            <tr>
		              <td align="center">
		                <table width="600" cellpadding="0" cellspacing="0" style="background-color:#151922;border-radius:12px;padding:32px;">

		              <tr>
		                <td style="font-size:20px;font-weight:600;color:#ffffff;padding-bottom:16px;">
		                  {title}
		                </td>
		              </tr>

		        {(tag is not null ? $"""

			                            <tr>
			                              <td style="padding-bottom:20px;">
			                                <span style="display:inline-block;background-color:#1f2937;color:#60a5fa;padding:6px 12px;border-radius:999px;font-size:12px;">
			                                  {tag}
			                                </span>
			                              </td>
			                            </tr>

			                            """ : string.Empty)}


		        {content}

		            </table>

		            <table width="600" cellpadding="0" cellspacing="0" style="margin-top:16px;">
		              <tr>
		                <td style="font-size:12px;color:#6b7280;text-align:center;">
		                  © 2026 cadr.studio · This is an automated system message
		                </td>
		              </tr>
		            </table>

		          </td>
		        </tr>

		        </table>
		        </body>
		        </html>

		        """;
	}
}