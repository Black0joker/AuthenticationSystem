using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Common.Helpers;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Domain.Entities;

namespace IdentityAuth.Application.Authentication.Services;

public class LoginService : ILoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityEventService _securityEventService;
    private readonly IApplicationDbContext _dbContext;

    public LoginService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ISecurityEventService securityEventService,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _securityEventService = securityEventService;
        _dbContext = dbContext;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        // Normalize email for lookup
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);

        // Find user by normalized email
        var user = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, cancellationToken);

        // Use generic error message to prevent user enumeration
        if (user is null)
        {
            await _securityEventService.LogEventAsync(
                SecurityEventType.LoginFailed,
                ipAddress: ipAddress,
                metadata: $"Email: {request.Email} - User not found",
                cancellationToken: cancellationToken);

            throw new AuthenticationException("Invalid email or password.");
        }

        // Check if account is locked
        if (user.IsLocked)
        {
            // Check if lockout has expired
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                throw new AccountLockedException("Account is temporarily locked. Please try again later.");
            }

            // Lockout has expired, unlock the account
            user.IsLocked = false;
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
        }

        // Verify password
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;

            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLocked = true;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);

                await _dbContext.SaveChangesAsync(cancellationToken);

                await _securityEventService.LogEventAsync(
                    SecurityEventType.AccountLocked,
                    userId: user.Id,
                    ipAddress: ipAddress,
                    metadata: "Account locked after 5 failed login attempts",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _dbContext.SaveChangesAsync(cancellationToken);

                await _securityEventService.LogEventAsync(
                    SecurityEventType.LoginFailed,
                    userId: user.Id,
                    ipAddress: ipAddress,
                    metadata: $"Failed attempt {user.FailedLoginAttempts}/5",
                    cancellationToken: cancellationToken);
            }

            // Generic error to prevent user enumeration
            throw new AuthenticationException("Invalid email or password.");
        }

        // Check if email is verified
        if (!user.IsEmailVerified)
        {
            throw new EmailNotVerifiedException("Please verify your email address before logging in.");
        }

        // Reset failed login attempts on successful login
        user.FailedLoginAttempts = 0;

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log successful login
        await _securityEventService.LogEventAsync(
            SecurityEventType.LoginSucceeded,
            userId: user.Id,
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        // Generate JWT access token
        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        // Generate refresh token
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, ipAddress, cancellationToken: cancellationToken);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            LastLoginAt = user.LastLoginAt.Value
        };
    }
}

public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}

public class AccountLockedException : Exception
{
    public AccountLockedException(string message) : base(message) { }
}

public class EmailNotVerifiedException : Exception
{
    public EmailNotVerifiedException(string message) : base(message) { }
}
