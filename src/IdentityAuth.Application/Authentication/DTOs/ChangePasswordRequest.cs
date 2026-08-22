using System.ComponentModel.DataAnnotations;

namespace IdentityAuth.Application.Authentication.DTOs;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one digit.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
