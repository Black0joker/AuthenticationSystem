using IdentityAuth.Application.Authentication.DTOs;

namespace IdentityAuth.Application.Authentication.Services;

public interface IEmailVerificationService
{
    /// <summary>
    /// Generates a verification token for a user and returns the plain-text token.
    /// </summary>
    Task<string> GenerateVerificationTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an email using the provided token.
    /// </summary>
    Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
}
