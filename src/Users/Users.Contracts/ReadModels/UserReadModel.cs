namespace Users.Contracts.ReadModels;

public sealed record UserReadModel(Guid Id, string Email, bool IsEmailConfirmed, DateTime LastLoggedInAt);