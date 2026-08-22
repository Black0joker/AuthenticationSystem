using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityAuth.Application.Authentication.Services;

public class AccountService : IAccountService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityEventService _securityEventService;

    public AccountService(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ISecurityEventService securityEventService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _securityEventService = securityEventService;
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user is null)
        {
            return new ChangePasswordResponse
            {
                Success = false,
                Message = "User not found."
            };
        }

        // Verify current password
        var isCurrentPasswordValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);

        if (!isCurrentPasswordValid)
        {
            return new ChangePasswordResponse
            {
                Success = false,
                Message = "Current password is incorrect."
            };
        }

        // Ensure new password is different from current
        if (request.NewPassword == request.CurrentPassword)
        {
            return new ChangePasswordResponse
            {
                Success = false,
                Message = "New password must be different from the current password."
            };
        }

        // Hash and update the password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Update security stamp to invalidate existing sessions
        user.SecurityStamp = Guid.NewGuid().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log security event
        await _securityEventService.LogEventAsync(
            SecurityEventType.PasswordChanged,
            userId: userId,
            cancellationToken: cancellationToken);

        return new ChangePasswordResponse
        {
            Success = true,
            Message = "Password changed successfully."
        };
    }

    public async Task<UpdateProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        // Update profile fields
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UpdatedAt = user.UpdatedAt.Value
        };
    }
}
