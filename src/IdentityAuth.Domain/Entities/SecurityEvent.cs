namespace IdentityAuth.Domain.Entities;

public enum SecurityEventType
{
    UserRegistered,
    LoginSucceeded,
    LoginFailed,
    AccountLocked,
    PasswordChanged,
    PasswordResetRequested,
    PasswordResetCompleted,
    EmailVerified,
    RefreshTokenCreated,
    RefreshTokenRevoked,
    RefreshTokenReuseDetected,
    Logout
}

public class SecurityEvent : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public SecurityEventType EventType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
}
