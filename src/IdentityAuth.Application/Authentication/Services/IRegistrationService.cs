using IdentityAuth.Application.Authentication.DTOs;

namespace IdentityAuth.Application.Authentication.Services;

public interface IRegistrationService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
