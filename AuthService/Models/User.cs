using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Stored as BCrypt hash. Column named "Password" to match the monolith DB schema.
    [Required, MaxLength(255)]
    public string Password { get; set; } = string.Empty;
}
