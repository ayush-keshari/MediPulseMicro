using AuthService.DTOs;

namespace AuthService.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> UpdateRoleAsync(int id, UpdateRoleRequest request);

    // Hard delete — physically removes the user row.
    // Returns false if user not found.
    Task<bool> DeleteUserAsync(int id);
}
