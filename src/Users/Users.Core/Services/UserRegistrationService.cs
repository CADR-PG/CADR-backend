using Users.Contracts.Events;
using Users.Core.Database;
using Users.Core.Entities;
using Wolverine.EntityFrameworkCore;

namespace Users.Core.Services;

internal sealed class UserRegistrationService(IDbContextOutbox<UsersDbContext> outbox)
{
	/// <summary>
	/// Saves the new user and the <see cref="UserRegistered" /> integration event in a single transaction
	/// (transactional outbox), then hands the event over to Wolverine for delivery.
	/// </summary>
	public async Task Register(User user, CancellationToken cancellationToken)
	{
		await outbox.DbContext.Users.AddAsync(user, cancellationToken);
		await outbox.PublishAsync(new UserRegistered(user.Id));
		await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
	}
}