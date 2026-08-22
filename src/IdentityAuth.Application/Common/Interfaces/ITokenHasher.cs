namespace IdentityAuth.Application.Common.Interfaces;

public interface ITokenHasher
{
    /// <summary>
    /// Hashes a token for secure storage. Tokens are never stored in plain text.
    /// </summary>
    string HashToken(string token);

    /// <summary>
    /// Verifies a token against its stored hash.
    /// </summary>
    bool VerifyToken(string token, string tokenHash);
}
