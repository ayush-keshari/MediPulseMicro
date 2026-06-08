namespace AuthService.DTOs;

// What we send back after a successful login.
// The Angular app stores this token and sends it as: Authorization: Bearer <token>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
