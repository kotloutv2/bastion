using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace RvmsModels;

public static class Security
{
    private const int SaltSize = 128;
    private const int HashIterationCount = 100000;
    private const int HashLengthBytes = 32;

    public const int MinimumPasswordLength = 8;

    public static string GenerateSalt(int size = SaltSize)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[size];
        rng.GetNonZeroBytes(salt);
        return Convert.ToBase64String(salt);
    }

    public static string HashPassword(string salt, string password)
    {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password,
            Convert.FromBase64String(salt),
            KeyDerivationPrf.HMACSHA256,
            HashIterationCount,
            HashLengthBytes
        ));
    }

    public static bool ValidatePassword(string salt, string hashedPassword, string password)
    {
        return HashPassword(salt, password) == hashedPassword;
    }
}