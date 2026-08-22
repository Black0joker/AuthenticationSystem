using IdentityAuth.Application.Authentication.DTOs;

namespace IdentityAuth.Application.Authentication.Services;

public interface IPasswordResetService
{
    /// <summary>
    /// Initiates the password reset flow. Generates a reset token for the user.
    /// Returns a generic message regardless of whether the email exists (prevents enumeration).
    /// </summary>
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// </summary>
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a password reset token for a user (used in testing).
    /// </summary>
    Task<string> GenerateResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
