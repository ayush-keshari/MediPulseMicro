using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

// Used by Admin to change a user's role via PUT /api/users/{id}/role
public class UpdateRoleRequest
{
    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;
}
