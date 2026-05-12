using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Must have uppercase, lowercase, digit, special character, min 8 chars — same rule as monolith.
    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must have at least 8 characters with uppercase, lowercase, digit, and special character.")]
    public string Password { get; set; } = string.Empty;

    // Exactly 10 digits — same rule as monolith.
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    public string? Phone { get; set; }

    // Optional — defaults to "User" if not provided. Admin can change it later via PUT /api/users/{id}/role.
    public string Role { get; set; } = "User";
}
