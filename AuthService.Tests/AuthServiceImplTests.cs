using Xunit;
using AuthService.Models;
using AuthService.Services;
using AuthService.DTOs;
using AuthService.Data;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task RegisterAsync_RegistersUserSuccessfully_WhenCredentialsAreValid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthServiceImpl(context);

        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            Name = "Test User",
            Role = "User"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.User);
        Assert.Equal("test@example.com", result.User.Email);
        Assert.Equal("Test User", result.User.Name);
        Assert.Equal("User", result.User.Role);
        Assert.NotNull(result.User.Id);

        // Verify password is hashed (not stored as plain text)
        var savedUser = await context.Users.FindAsync(result.User.Id);
        Assert.NotEqual("SecurePassword123!", savedUser.PasswordHash);
        Assert.NotNull(savedUser.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsError_WhenEmailAlreadyExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthServiceImpl(context);

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            Name = "Existing User",
            Role = "User"
        };

        // Create first user
        await service.RegisterAsync(request);

        // Try to register with same email
        var duplicateRequest = new RegisterRequest
        {
            Email = "existing@example.com", // Same email
            Password = "AnotherPassword456!",
            Name = "Duplicate User",
            Role = "Admin"
        };

        // Act
        var result = await service.RegisterAsync(duplicateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.ErrorMessage);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthServiceImpl(context);

        // Create a user first
        var registerRequest = new RegisterRequest
        {
            Email = "login@example.com",
            Password = "SecurePassword123!",
            Name = "Login User",
            Role = "User"
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
        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.NotNull(result.User);
        Assert.Equal("login@example.com", result.User.Email);
        Assert.Equal("Login User", result.User.Name);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenEmailDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthServiceImpl(context);

        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid", result.ErrorMessage);
        Assert.Null(result.Token);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenPasswordIsInvalid()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new AuthServiceImpl(context);

        // Create a user first
        var registerRequest = new RegisterRequest
        {
            Email = "wrongpass@example.com",
            Password = "CorrectPassword123!",
            Name = "Wrong Pass User",
            Role = "User"
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
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid", result.ErrorMessage);
        Assert.Null(result.Token);
        Assert.Null(result.User);
    }
}