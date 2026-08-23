using IdentityAuth.Application.Authentication.DTOs;
using IdentityAuth.Application.Common.Helpers;
using IdentityAuth.Application.Common.Interfaces;
using IdentityAuth.Application.Common.Validators;
using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityAuth.Application.Authentication.Services;

public class RegistrationService : IRegistrationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityEventService _securityEventService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISecurityEventService securityEventService,
        IApplicationDbContext dbContext,
        IEmailVerificationService emailVerificationService,
        ILogger<RegistrationService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _securityEventService = securityEventService;
        _dbContext = dbContext;
        _emailVerificationService = emailVerificationService;
        _logger = logger;
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

        // Generate email verification token
        var verificationToken = await _emailVerificationService.GenerateVerificationTokenAsync(user.Id, cancellationToken);

        // Log the verification token (in production, this would be sent via email)
        _logger.LogInformation("Email verification token generated for user {UserId} ({Email}): {Token}",
            user.Id, user.Email, verificationToken);

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
