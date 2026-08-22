namespace IdentityAuth.Application.Authentication.DTOs;

public class VerifyEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
