using System.Security.Cryptography;
using IdentityAuth.Application.Common.Interfaces;

namespace IdentityAuth.Infrastructure.Security;

/// <summary>
/// Password hasher using PBKDF2 (RFC 2898) with HMAC-SHA256.
/// Format: {iterations}.{salt_base64}.{hash_base64}
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 3)
                return false;

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                Algorithm,
                hash.Length);

            return CryptographicOperations.FixedTimeEquals(computedHash, hash);
        }
        catch
        {
            return false;
        }
    }
}
