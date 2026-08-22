using System.ComponentModel.DataAnnotations;

namespace IdentityAuth.Application.Authentication.DTOs;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}
