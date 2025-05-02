using Microsoft.AspNetCore.Identity;

namespace API.Modules.Users.Services;

internal static class HashingService
{
	private static readonly PasswordHasher<object> Hasher = new();

	public static string Hash(string value) => Hasher.HashPassword(null!, value);

	public static bool IsValid(string value, string hashedValue)
		=> Hasher.VerifyHashedPassword(null!, hashedValue, value) is PasswordVerificationResult.Success;
}