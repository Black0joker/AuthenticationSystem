using Microsoft.Extensions.DependencyInjection;

namespace IdentityAuth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application layer services will be registered here
        // as they are implemented in subsequent phases.

        return services;
    }
}
