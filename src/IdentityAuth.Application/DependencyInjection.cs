using IdentityAuth.Application.Authentication.Services;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Application.Security.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        // Register security services
        services.AddScoped<ISecurityEventService, SecurityEventService>();

        return services;
    }
}
