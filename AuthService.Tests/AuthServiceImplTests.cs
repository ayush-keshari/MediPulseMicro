using Xunit;
using AuthService.Models;

namespace AuthService.Tests;

public class AuthServiceTests
{
    [Fact]
    public void User_HasEmail()
    {
        var user = new User { Email = "test@example.com" };
        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public void User_HasRole()
    {
        var user = new User { Role = "Admin" };
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public void User_HasName()
    {
        var user = new User { Name = "John Doe" };
        Assert.Equal("John Doe", user.Name);
    }
}