using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Authentication.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IApplicationDbContext _dbContext;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISecurityEventService _securityEventService;

    public RefreshTokenService(
        IApplicationDbContext dbContext,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IJwtTokenService jwtTokenService,
        ISecurityEventService securityEventService)
    {
        _dbContext = dbContext;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _jwtTokenService = jwtTokenService;
        _securityEventService = securityEventService;
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, string? ipAddress = null, Guid? familyId = null, CancellationToken cancellationToken = default)
    {
        var plainToken = _tokenGenerator.GenerateToken(64);
        var tokenHash = _tokenHasher.HashToken(plainToken);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
            CreatedByIp = ipAddress,
            FamilyId = familyId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        //await _dbContext.SaveChangesAsync(cancellationToken);

        return plainToken;
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        // Hash the incoming token to look it up
        var tokenHash = _tokenHasher.HashToken(request.RefreshToken);

        // Find the refresh token
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is null)
        {
            throw new InvalidRefreshTokenException("Invalid refresh token.");
        }

        // REUSE DETECTION: If the token has been revoked, it means it was already used.
        // This indicates a potential token theft. Revoke all tokens in the family.
        if (storedToken.IsRevoked)
        {
            // Revoke all tokens for this user (security breach detected)
            await _refreshTokenRepository.RevokeAllUserTokensAsync(storedToken.UserId, ipAddress, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Log reuse detection
            await _securityEventService.LogEventAsync(
                SecurityEventType.RefreshTokenReuseDetected,
                userId: storedToken.UserId,
                ipAddress: ipAddress,
                metadata: $"FamilyId: {storedToken.FamilyId}",
                cancellationToken: cancellationToken);

            throw new RefreshTokenReuseException("Refresh token reuse detected. All sessions have been revoked.");
        }

        // Check if token has expired
        if (storedToken.IsExpired)
        {
            throw new InvalidRefreshTokenException("Refresh token has expired.");
        }

        // Get the user
        var user = storedToken.User;

        // ROTATION: Revoke the current token and create a new one
        await _refreshTokenRepository.RevokeAsync(storedToken, ipAddress, cancellationToken);

        // Log token revocation
        await _securityEventService.LogEventAsync(
            SecurityEventType.RefreshTokenRevoked,
            userId: storedToken.UserId,
            ipAddress: ipAddress,
            metadata: "Token rotated",
            cancellationToken: cancellationToken);

        // Generate new refresh token in the same family
        var newPlainToken = await GenerateRefreshTokenAsync(
            storedToken.UserId,
            ipAddress,
            storedToken.FamilyId,
            cancellationToken);

        // Link the old token to the new one
        storedToken.ReplacedByTokenId = null; // Will be set after we know the new token's ID

        // Generate new access token
        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        user.SecurityStamp=Guid.NewGuid().ToString(); // Invalidate existing sessions (if using SecurityStamp for session validation)

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newPlainToken
        };
    }

    /// <summary>
    /// Revokes a specific refresh token (used during logout).
    /// </summary>
    public async Task RevokeTokenAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenHasher.HashToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is not null && storedToken.IsActive)
        {
            await _refreshTokenRepository.RevokeAsync(storedToken, ipAddress, cancellationToken);

            await _securityEventService.LogEventAsync(
                SecurityEventType.Logout,
                userId: storedToken.UserId,
                ipAddress: ipAddress,
                cancellationToken: cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException(string message) : base(message) { }
}

public class RefreshTokenReuseException : Exception
{
    public RefreshTokenReuseException(string message) : base(message) { }
}
