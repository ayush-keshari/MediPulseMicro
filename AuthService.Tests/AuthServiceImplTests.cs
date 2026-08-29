using Xunit;
using AuthService.Models;
using AuthService.Services;
using AuthService.DTOs;
using AuthService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Shared.Exceptions;

namespace AuthService.Tests;

public class AuthServiceImplTests
{
    private AuthDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AuthDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private IConfiguration GetTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jwt:Key", "test-secret-key-12345678901234567890123456789012"),
                new KeyValuePair<string, string?>("Jwt:Issuer", "test-issuer"),
                new KeyValuePair<string, string?>("Jwt:Audience", "test-audience"),
                new KeyValuePair<string, string?>("Jwt:ExpiryHours", "1")
            })
            .Build();
    }

    [Fact]
    public async Task RegisterAsync_RegistersUserSuccessfully_WhenCredentialsAreValid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var config = GetTestConfiguration();
        var service = new AuthServiceImpl(context, config);

        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            Name = "Test User"
            // Role is internal and set to "Unassigned" by the service
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("Unassigned", result.Role);
        Assert.True(result.UserId > 0);

        // Verify password is hashed (not stored as plain text)
        var savedUser = await context.Users.FindAsync(result.UserId);
        Assert.NotEqual("SecurePassword123!", savedUser!.Password);
        Assert.NotNull(savedUser!.Password);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsError_WhenEmailAlreadyExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var config = GetTestConfiguration();
        var service = new AuthServiceImpl(context, config);

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            Name = "Existing User"
            // Role is internal and set to "Unassigned" by the service
        };

        // Create first user
        await service.RegisterAsync(request);

        // Try to register with same email
        var duplicateRequest = new RegisterRequest
        {
            Email = "existing@example.com", // Same email
            Password = "AnotherPassword456!",
            Name = "Duplicate User"
            // Role is internal
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await service.RegisterAsync(duplicateRequest));
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var config = GetTestConfiguration();
        var service = new AuthServiceImpl(context, config);

        // Create a user first
        var registerRequest = new RegisterRequest
        {
            Email = "login@example.com",
            Password = "SecurePassword123!",
            Name = "Login User"
            // Role is internal and set to "Unassigned" by the service
        };
        await service.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal("login@example.com", result.Email);
        Assert.Equal("Login User", result.Name);
        Assert.Equal("Unassigned", result.Role);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenEmailDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var config = GetTestConfiguration();
        var service = new AuthServiceImpl(context, config);

        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenPasswordIsInvalid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var config = GetTestConfiguration();
        var service = new AuthServiceImpl(context, config);

        // Create a user first
        var registerRequest = new RegisterRequest
        {
            Email = "wrongpass@example.com",
            Password = "CorrectPassword123!",
            Name = "Wrong Pass User"
            // Role is internal and set to "Unassigned" by the service
        };
        await service.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "wrongpass@example.com",
            Password = "WrongPassword456!" // Incorrect password
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_UpgradesLegacyPlainTextPassword()
    {
        await using var context = CreateInMemoryDbContext();
        context.Users.Add(new User
        {
            Email = "legacy@example.com",
            Name = "Legacy User",
            Role = "Unassigned",
            Password = "LegacyPassword123!"
        });
        await context.SaveChangesAsync();
        var service = new AuthServiceImpl(context, GetTestConfiguration());

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "legacy@example.com",
            Password = "LegacyPassword123!"
        });

        Assert.NotNull(result);
        Assert.StartsWith("$2", (await context.Users.SingleAsync()).Password);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUsersInDescendingIdOrder()
    {
        await using var context = CreateInMemoryDbContext();
        context.Users.AddRange(
            new User { Name = "First", Email = "first@example.com", Role = "User", Password = "hash" },
            new User { Name = "Second", Email = "second@example.com", Role = "Admin", Password = "hash" });
        await context.SaveChangesAsync();
        var service = new AuthServiceImpl(context, GetTestConfiguration());

        var result = await service.GetAllUsersAsync();

        Assert.Equal(new[] { "Second", "First" }, result.Select(user => user.Name));
        Assert.DoesNotContain(result, user => user.Email == "" && user.Name == "");
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFalseForMissingUser_AndUpdatesExistingUser()
    {
        await using var context = CreateInMemoryDbContext();
        var user = new User { Name = "User", Email = "user@example.com", Role = "Unassigned", Password = "hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new AuthServiceImpl(context, GetTestConfiguration());

        Assert.False(await service.UpdateRoleAsync(999, new UpdateRoleRequest { Role = "Admin" }));
        Assert.True(await service.UpdateRoleAsync(user.UserId, new UpdateRoleRequest { Role = "Admin" }));
        Assert.Equal("Admin", (await context.Users.FindAsync(user.UserId))!.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_RejectsDuplicateEmail()
    {
        await using var context = CreateInMemoryDbContext();
        context.Users.AddRange(
            new User { Name = "One", Email = "one@example.com", Role = "User", Password = "hash" },
            new User { Name = "Two", Email = "two@example.com", Role = "User", Password = "hash" });
        await context.SaveChangesAsync();
        var user = await context.Users.SingleAsync(u => u.Email == "two@example.com");
        var service = new AuthServiceImpl(context, GetTestConfiguration());

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateUserAsync(user.UserId, new UpdateUserRequest
        {
            Name = "Two",
            Email = "one@example.com",
            Role = "User"
        }));
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalseForMissingUser_AndDeletesExistingUser()
    {
        await using var context = CreateInMemoryDbContext();
        var user = new User { Name = "User", Email = "user@example.com", Role = "User", Password = "hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = new AuthServiceImpl(context, GetTestConfiguration());

        Assert.False(await service.DeleteUserAsync(999));
        Assert.True(await service.DeleteUserAsync(user.UserId));
        Assert.Null(await context.Users.FindAsync(user.UserId));
    }
}
