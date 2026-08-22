using IdentityAuth.Application.Authentication.Services;
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

        return services;
    }
}
