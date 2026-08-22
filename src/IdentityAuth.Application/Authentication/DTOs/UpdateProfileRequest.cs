using System.ComponentModel.DataAnnotations;

namespace IdentityAuth.Application.Authentication.DTOs;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name must not exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name must not exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;
}
