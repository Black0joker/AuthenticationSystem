using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Common.Helpers;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Application.Common.Validators;
using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityAuth.Application.Authentication.Services;

public class RegistrationService : IRegistrationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityEventService _securityEventService;
    private readonly IApplicationDbContext _dbContext;

    public RegistrationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISecurityEventService securityEventService,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _securityEventService = securityEventService;
        _dbContext = dbContext;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Validate password strength
        var (isValid, errors) = PasswordValidator.Validate(request.Password);
        if (!isValid)
        {
            throw new ArgumentException(string.Join(" ", errors));
        }

        // Normalize email for consistent comparison
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);

        // Check for duplicate email
        var existingUser = await _userRepository.ExistsByEmailNormalizedAsync(normalizedEmail, cancellationToken);
        if (existingUser)
        {
            throw new DuplicateEmailException("An account with this email already exists.");
        }

        // Hash the password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create the user entity
        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHash,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsEmailVerified = false,
            IsLocked = false,
            FailedLoginAttempts = 0,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        // Persist the user
        await _userRepository.AddAsync(user, cancellationToken);

        // Assign default role
        await AssignDefaultRoleAsync(user, cancellationToken);

        // Save all changes
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log security event
        await _securityEventService.LogEventAsync(
            SecurityEventType.UserRegistered,
            userId: user.Id,
            metadata: $"Email: {user.Email}",
            cancellationToken: cancellationToken);

        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task AssignDefaultRoleAsync(User user, CancellationToken cancellationToken)
    {
        var defaultRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == "USER", cancellationToken);

        if (defaultRole != null)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = defaultRole.Id
            });
        }
    }
}

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string message) : base(message) { }
}
