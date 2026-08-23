using IdentityAuth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityAuth.Infrastructure.Services;

/// <summary>
/// Background service that periodically removes expired and used tokens
/// (refresh, email verification, password reset) to prevent unbounded table growth.
/// </summary>
public class TokenCleanupService : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _interval;

    public TokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<TokenCleanupService> logger,
        TimeSpan? interval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval ?? DefaultInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenCleanupService started. Cleanup interval: {Interval}", _interval);

        // Delay initial execution to allow the application to start up
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("TokenCleanupService stopping.");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Remove expired refresh tokens
        var expiredRefreshTokens = await dbContext.RefreshTokens
            .Where(t => t.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredRefreshTokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(expiredRefreshTokens);
            _logger.LogInformation("Removing {Count} expired refresh tokens", expiredRefreshTokens.Count);
        }

        // Remove expired or used email verification tokens
        var expiredVerificationTokens = await dbContext.EmailVerificationTokens
            .Where(t => t.ExpiresAt < now || t.UsedAt != null)
            .ToListAsync(cancellationToken);

        if (expiredVerificationTokens.Count > 0)
        {
            dbContext.EmailVerificationTokens.RemoveRange(expiredVerificationTokens);
            _logger.LogInformation("Removing {Count} expired/used email verification tokens", expiredVerificationTokens.Count);
        }

        // Remove expired or used password reset tokens
        var expiredResetTokens = await dbContext.PasswordResetTokens
            .Where(t => t.ExpiresAt < now || t.UsedAt != null)
            .ToListAsync(cancellationToken);

        if (expiredResetTokens.Count > 0)
        {
            dbContext.PasswordResetTokens.RemoveRange(expiredResetTokens);
            _logger.LogInformation("Removing {Count} expired/used password reset tokens", expiredResetTokens.Count);
        }

        var totalRemoved = expiredRefreshTokens.Count + expiredVerificationTokens.Count + expiredResetTokens.Count;

        if (totalRemoved > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Token cleanup completed. Removed {Total} tokens in total.", totalRemoved);
        }
        else
        {
            _logger.LogDebug("Token cleanup completed. No expired tokens found.");
        }
    }
}
