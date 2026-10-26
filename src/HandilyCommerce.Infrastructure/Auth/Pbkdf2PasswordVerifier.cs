using System.Security.Cryptography;
using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Infrastructure.Auth;

/// <summary>
/// PBKDF2-SHA256 verifier for hashes in the format <c>pbkdf2-sha256$iterations$saltBase64$hashBase64</c>.
/// Compares with <see cref="CryptographicOperations.FixedTimeEquals"/>.
/// </summary>
public sealed class Pbkdf2PasswordVerifier : IPasswordVerifier
{
    public const string Prefix = "pbkdf2-sha256";
    private const int MaxIterations = 2_000_000;

    public bool Verify(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Trim().Split('$');
        if (parts.Length != 4 || parts[0] != Prefix
            || !int.TryParse(parts[1], out var iterations) || iterations is < 1 or > MaxIterations)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length == 0 || expected.Length == 0)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Builds a hash string (used by tests and to document the format).</summary>
    public static string Hash(string password, int iterations = 210_000)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
        return $"{Prefix}${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
