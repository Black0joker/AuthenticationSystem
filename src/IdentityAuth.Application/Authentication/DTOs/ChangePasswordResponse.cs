namespace IdentityAuth.Application.Authentication.DTOs;

public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
