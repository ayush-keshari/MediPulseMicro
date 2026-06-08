using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services;

public class AuthServiceImpl : IAuthService
{
    private readonly AuthDbContext _db;
    private readonly IConfiguration _config;

    public AuthServiceImpl(AuthDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return null;

        // Support both BCrypt-hashed passwords (start with "$2") and legacy plain-text.
        // This matches the monolith's login logic so existing users aren't locked out.
        bool passwordValid = false;
        bool isBcryptHash = user.Password.StartsWith("$2");

        if (isBcryptHash)
        {
            try { passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password); }
            catch { passwordValid = false; }
        }
        else
        {
            // Legacy plain-text: compare directly, then upgrade to BCrypt on the fly
            passwordValid = user.Password == request.Password;
            if (passwordValid)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                await _db.SaveChangesAsync();
            }
        }

        if (!passwordValid) return null;

        var expiryHours = int.Parse(_config["Jwt:ExpiryHours"]!);
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        return new AuthResponse
        {
            Token     = GenerateJwtToken(user, expiresAt),
            Name      = user.Name,
            Email     = user.Email,
            Role      = user.Role,
            ExpiresAt = expiresAt
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
            throw new InvalidOperationException("A user with this email already exists.");

        // Role is intentionally hardcoded to "Unassigned" on this public endpoint —
        // never trust a client-sent role here. Admins assign the real role via
        // PUT /api/users/{id}/role immediately after creation in the admin UI.
        var user = new User
        {
            Name     = request.Name,
            Email    = request.Email,
            Phone    = request.Phone,
            Role     = "Unassigned",
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Return the created user (with the EF-generated UserId) so the admin UI
        // can chain the role-assignment call. The frontend's add-user flow depends
        // on this UserId to call updateUserRole(id, role) right after register.
        return MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _db.Users
            .OrderByDescending(u => u.UserId)
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<bool> UpdateRoleAsync(int id, UpdateRoleRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        user.Role = request.Role;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        // Pre-check duplicate email (case-insensitive, excluding this user).
        // Mirrors RegisterAsync so the UI gets a clean 409 instead of a raw DB
        // unique-index error from IX_User_Email. Controller also catches
        // DbUpdateException as a safety net.
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            await _db.Users.AnyAsync(u => u.UserId != id &&
                                          u.Email.ToLower() == request.Email.ToLower()))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        user.Name  = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Role  = request.Role;

        // Password is optional on edit — only re-hash when the admin actually
        // typed a new one. Empty/whitespace/null means "keep the existing hash".
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        // Hard delete — physically removes the row, matching the monolith's Admin Delete logic.
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private string GenerateJwtToken(User user, DateTime expiresAt)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name,               user.Name),
            new Claim(ClaimTypes.Role,               user.Role)
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToDto(User user) => new()
    {
        UserId = user.UserId,
        Name   = user.Name,
        Email  = user.Email,
        Role   = user.Role,
        Phone  = user.Phone
    };
}
