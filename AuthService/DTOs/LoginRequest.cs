using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

// What the Angular frontend POSTs to /api/auth/login
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
