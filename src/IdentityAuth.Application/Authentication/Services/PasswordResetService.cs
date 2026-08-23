using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Common.Helpers;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IdentityAuth.Application.Authentication.Services;

public class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly ISecurityEventService _securityEventService;
    private readonly IApplicationDbContext _dbContext;


    public PasswordResetService(ILogger<PasswordResetService> logger,
        IUserRepository userRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        ISecurityEventService securityEventService,
        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _userRepository = userRepository;
        _resetTokenRepository = resetTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _securityEventService = securityEventService;
        _dbContext = dbContext;
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Normalize email for lookup
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);

        // Find user by normalized email
        var user = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, cancellationToken);

        // Always return the same message to prevent user enumeration
        if (user is null)
        {
            return new ForgotPasswordResponse
            {
                Message = "If an account with that email exists, a password reset link has been sent."
            };
        }

        // Invalidate any existing reset tokens for this user
        await _resetTokenRepository.InvalidateAllUserTokensAsync(user.Id, cancellationToken);

        // Generate new reset token
        var resetToken=await GenerateResetTokenAsync(user.Id, cancellationToken);

        _logger.LogInformation($"the reset token of the email : {user.Email} ==> {resetToken}");

        // Log security event
        await _securityEventService.LogEventAsync(
            SecurityEventType.PasswordResetRequested,
            userId: user.Id,
            metadata: $"Email: {user.Email}",
            cancellationToken: cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);



        return new ForgotPasswordResponse
        {
            Message = "If an account with that email exists, a password reset link has been sent."
        };
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Hash the incoming token to look it up
        var tokenHash = _tokenHasher.HashToken(request.Token);

        // Find the reset token
        var resetToken = await _resetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null)
        {
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "Invalid or expired reset token."
            };
        }

        // Check if token has been used
        if (resetToken.IsUsed)
        {
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "This reset token has already been used."
            };
        }

        // Check if token has expired
        if (resetToken.IsExpired)
        {
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "Invalid or expired reset token."
            };
        }

        // Get the user
        var user = await _dbContext.Users.FindAsync(new object[] { resetToken.UserId }, cancellationToken);

        if (user is null)
        {
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "Invalid or expired reset token."
            };
        }

        // Hash the new password and update the user
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Mark the token as used
        resetToken.UsedAt = DateTime.UtcNow;

        // Invalidate all other reset tokens for this user
        await _resetTokenRepository.InvalidateAllUserTokensAsync(user.Id, cancellationToken);

        // Re-mark the current token as used
        resetToken.UsedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log security event
        await _securityEventService.LogEventAsync(
            SecurityEventType.PasswordResetCompleted,
            userId: user.Id,
            cancellationToken: cancellationToken);

        return new ResetPasswordResponse
        {
            Success = true,
            Message = "Password has been reset successfully."
        };
    }

    public async Task<string> GenerateResetTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var plainToken = _tokenGenerator.GenerateToken(32);
        var tokenHash = _tokenHasher.HashToken(plainToken);

        var resetToken = new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(ResetTokenLifetime),
            CreatedAt = DateTime.UtcNow
        };

        await _resetTokenRepository.AddAsync(resetToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return plainToken;
    }
}
