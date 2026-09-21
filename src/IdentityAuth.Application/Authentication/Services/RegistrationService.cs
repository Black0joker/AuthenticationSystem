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
            FirstName = InputSanitizer.Sanitize(request.FirstName),
            LastName = InputSanitizer.Sanitize(request.LastName),
            IsEmailVerified = false,
            IsLocked = false,
            FailedLoginAttempts = 0,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        // Wrap user creation, role assignment, and token generation in a transaction
        // to prevent inconsistent state if any step fails.
        // Transactions are only supported on relational providers (skipped for InMemory in tests).
        try
        {
            if (_dbContext.Database.IsRelational())
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        await ExecuteRegistrationCoreAsync(user, cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                });
            }
            else
            {
                await ExecuteRegistrationCoreAsync(user, cancellationToken);
            }
        }
        catch (DbUpdateException ex)
        {
            // Handle race condition: another request inserted the same email between
            // our ExistsByEmailNormalizedAsync check and the actual INSERT.
            // The unique index on NormalizedEmail catches this at the DB level.
            _logger.LogWarning(ex, "Concurrent registration detected for email {Email}", normalizedEmail);
            throw new DuplicateEmailException("An account with this email already exists.");
        }

        // Log security event outside the transaction (non-critical, should not fail registration)
        try
        {
            await _securityEventService.LogEventAsync(
                SecurityEventType.UserRegistered,
                userId: user.Id,
                metadata: $"Email: {user.Email}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log security event for user registration {UserId}", user.Id);
        }

        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task ExecuteRegistrationCoreAsync(User user, CancellationToken cancellationToken)
    {
        // Persist the user
        await _userRepository.AddAsync(user, cancellationToken);

        // Assign default role
        await AssignDefaultRoleAsync(user, cancellationToken);

        // Save user and role
        //await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate email verification token (participates in the same transaction)
        var verificationToken = await _emailVerificationService.GenerateVerificationTokenAsync(user.Id, cancellationToken);

        // Log the verification token (in production, this would be sent via email)
        _logger.LogInformation("Email verification token generated for user {UserId} ({Email}): {Token}",
            user.Id, user.Email, verificationToken);
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
