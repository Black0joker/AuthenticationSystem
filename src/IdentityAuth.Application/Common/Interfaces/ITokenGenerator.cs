namespace IdentityAuth.Application.Common.Interfaces;

public interface ITokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// Returns the plain-text token (to be sent to the user).
    /// </summary>
    string GenerateToken(int sizeInBytes = 32);
}
