using System.Security.Cryptography;
using IdentityAuth.Application.Common.Interfaces;

namespace IdentityAuth.Infrastructure.Security;

/// <summary>
/// Generates cryptographically secure random tokens using RandomNumberGenerator.
/// Tokens are returned as URL-safe Base64 strings.
/// </summary>
public class TokenGenerator : ITokenGenerator
{
    public string GenerateToken(int sizeInBytes = 32)
    {
        if (sizeInBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Size must be positive.");

        var bytes = RandomNumberGenerator.GetBytes(sizeInBytes);

        // Use URL-safe Base64 encoding (replace + with -, / with _, remove =)
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
