using IdentityAuth.Application.Authentication.DTOs;

namespace IdentityAuth.Application.Authentication.Services;

public interface IAccountService
{
    /// <summary>
    /// Changes the authenticated user's password after verifying the current password.
    /// </summary>
    Task<ChangePasswordResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the authenticated user's profile (first name, last name).
    /// </summary>
    Task<UpdateProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
