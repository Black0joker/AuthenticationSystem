using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Application.Common.Settings;
using IdentityAuth.Infrastructure.Persistence;
using IdentityAuth.Infrastructure.Persistence.Repositories;
using IdentityAuth.Infrastructure.Security;
using IdentityAuth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure EF Core with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });

            // Enable sensitive data logging only in development
            if (configuration.GetValue<bool>("EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();

        // Register security services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<ITokenHasher, TokenHasher>();

        // Configure JWT settings
        var jwtSettings = new JwtSettings();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);
        services.AddSingleton(jwtSettings);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Register background token cleanup service
        services.AddHostedService<TokenCleanupService>();

        return services;
    }
}
