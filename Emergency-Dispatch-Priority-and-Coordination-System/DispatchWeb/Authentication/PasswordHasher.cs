using System.Security.Cryptography;

namespace DispatchWeb.Authentication;

/// <summary>Creates and verifies salted password hashes using .NET's built-in PBKDF2 implementation.</summary>
public sealed class PasswordHasher
{
    public const int DefaultIterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public PasswordHash Create(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        // A new random salt means two users with the same password receive different stored hashes.
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Hash(password, salt, DefaultIterations);
        return new PasswordHash(Convert.ToBase64String(hash), Convert.ToBase64String(salt), DefaultIterations);
    }

    public bool Verify(string password, string storedHash, string storedSalt, int iterations)
    {
        if (string.IsNullOrEmpty(password)) return false;

        try
        {
            var expectedHash = Convert.FromBase64String(storedHash);
            var salt = Convert.FromBase64String(storedSalt);
            var suppliedHash = Hash(password, salt, iterations);

            // FixedTimeEquals avoids returning earlier for partly matching hashes.
            return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Hash(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
}

public sealed record PasswordHash(string Hash, string Salt, int Iterations);
