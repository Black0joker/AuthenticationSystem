using System.Security.Cryptography;
using System.Text;
using IdentityAuth.Application.Common.Interfaces;

namespace IdentityAuth.Infrastructure.Security;

/// <summary>
/// Hashes tokens using SHA-256 for secure storage.
/// Tokens are never stored in plain text in the database.
/// </summary>
public class TokenHasher : ITokenHasher
{
    public string HashToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentNullException(nameof(token));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public bool VerifyToken(string token, string tokenHash)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tokenHash))
            return false;

        try
        {
            var computedHash = HashToken(token);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(tokenHash));
        }
        catch
        {
            return false;
        }
    }
}
