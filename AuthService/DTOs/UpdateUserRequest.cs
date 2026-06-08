using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

// Used by Admin to edit ANY field of a user via PUT /api/users/{id}.
// Mirrors RegisterRequest validation rules so the Edit Profile modal
// can enforce identical client-side patterns. Password is optional —
// when omitted/blank the existing BCrypt hash is preserved.
public class UpdateUserRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    public string? Phone { get; set; }

    // Optional new password — same complexity rule as Register.
    // Validation only fires when the admin actually provides a value;
    // an empty/null string means "keep the current password".
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must have at least 8 characters with uppercase, lowercase, digit, and special character.")]
    public string? Password { get; set; }
}
