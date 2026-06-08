using AuthService.DTOs;

namespace AuthService.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<bool> UpdateRoleAsync(int id, UpdateRoleRequest request);

    // Admin-only full-profile edit. Updates name/email/phone/role and,
    // if password is provided, re-hashes and stores it. Returns null
    // if user not found, throws InvalidOperationException on email collision.
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request);

    // Hard delete — physically removes the user row.
    // Returns false if user not found.
    Task<bool> DeleteUserAsync(int id);
}
