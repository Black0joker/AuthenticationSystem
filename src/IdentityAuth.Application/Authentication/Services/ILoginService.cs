using IdentityAuth.Application.Authentication.DTOs;

namespace IdentityAuth.Application.Authentication.Services;

public interface ILoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
