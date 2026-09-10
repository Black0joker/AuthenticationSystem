using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityAuth.Application.Authentication.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;

    public EmailVerificationService(
        IApplicationDbContext dbContext,
        IEmailVerificationTokenRepository tokenRepository,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher)
    {
        _dbContext = dbContext;
        _tokenRepository = tokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
    }

    public async Task<string> GenerateVerificationTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {

        // Invalidate any existing tokens for this user
        await _tokenRepository.InvalidateUserTokensAsync(userId, cancellationToken);

        // Generate a new secure token
        var plainToken = _tokenGenerator.GenerateToken(32);
        var tokenHash = _tokenHasher.HashToken(plainToken);

        // Create and store the token entity
        var verificationToken = new EmailVerificationToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(TokenLifetime),
            CreatedAt = DateTime.UtcNow
        };

        await _tokenRepository.AddAsync(verificationToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return plainToken;
    }

    public async Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        // Hash the incoming token to look it up
        var tokenHash = _tokenHasher.HashToken(request.Token);

        // Find the token
        var verificationToken = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (verificationToken is null)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "Invalid verification token."
            };
        }

        // Check if token has already been used (single-use enforcement)
        if (verificationToken.IsUsed)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "This verification token has already been used."
            };
        }

        // Check if token has expired
        if (verificationToken.IsExpired)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "This verification token has expired."
            };
        }

        // Mark token as used (single-use enforcement)
        verificationToken.UsedAt = DateTime.UtcNow;

        // Mark user's email as verified
        var user = verificationToken.User;

        if (user is null)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "User associated with this token no longer exists."
            };
        }

        user.IsEmailVerified = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VerifyEmailResponse
        {
            Success = true,
            Message = "Email verified successfully."
        };
    }
}
