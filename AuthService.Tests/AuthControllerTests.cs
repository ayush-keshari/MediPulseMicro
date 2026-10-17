using System.Security.Claims;
using AuthService.Controllers;
using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Exceptions;

namespace AuthService.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_ReturnsUnauthorizedForInvalidCredentials_AndOkForValidCredentials()
    {
        var service = new Mock<IAuthService>();
        service.Setup(s => s.LoginAsync(It.Is<LoginRequest>(r => r.Email == "bad@example.com")))
            .ReturnsAsync((AuthResponse?)null);
        service.Setup(s => s.LoginAsync(It.Is<LoginRequest>(r => r.Email == "good@example.com")))
            .ReturnsAsync(new AuthResponse { Email = "good@example.com", Token = "token" });
        var controller = new AuthController(service.Object);

        Assert.IsType<UnauthorizedObjectResult>(await controller.Login(new LoginRequest { Email = "bad@example.com" }));
        Assert.IsType<OkObjectResult>(await controller.Login(new LoginRequest { Email = "good@example.com" }));
    }

    [Fact]
    public async Task Register_ReturnsCreated_AndMapsDuplicateToConflict()
    {
        var service = new Mock<IAuthService>();
        service.Setup(s => s.RegisterAsync(It.Is<RegisterRequest>(r => r.Email == "new@example.com")))
            .ReturnsAsync(new UserDto { UserId = 7, Email = "new@example.com" });
        service.Setup(s => s.RegisterAsync(It.Is<RegisterRequest>(r => r.Email == "duplicate@example.com")))
            .ThrowsAsync(new BusinessRuleException("Email already exists."));
        var controller = new AuthController(service.Object);

        var created = Assert.IsType<ObjectResult>(await controller.Register(new RegisterRequest { Email = "new@example.com" }));
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.IsType<ConflictObjectResult>(await controller.Register(new RegisterRequest { Email = "duplicate@example.com" }));
    }

    [Fact]
    public void GetMe_ReturnsClaimsFromAuthenticatedPrincipal()
    {
        var controller = new AuthController(new Mock<IAuthService>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "42"),
                        new Claim(ClaimTypes.Name, "Ayush"),
                        new Claim(ClaimTypes.Email, "ayush@example.com"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "test"))
                }
            }
        };

        var result = Assert.IsType<OkObjectResult>(controller.GetMe());
        Assert.NotNull(result.Value);
        Assert.Equal("ayush@example.com", result.Value.GetType().GetProperty("Email")!.GetValue(result.Value));
        Assert.Equal("Admin", result.Value.GetType().GetProperty("Role")!.GetValue(result.Value));
    }

    [Fact]
    public async Task UsersController_CoversAdminMutationsAndSelfDeleteProtection()
    {
        var service = new Mock<IAuthService>();
        service.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<UserDto>());
        service.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync((UserDto?)null);
        service.Setup(s => s.GetUserByIdAsync(2)).ReturnsAsync(new UserDto { UserId = 2 });
        service.Setup(s => s.UpdateRoleAsync(3, It.IsAny<UpdateRoleRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateRoleAsync(4, It.IsAny<UpdateRoleRequest>())).ReturnsAsync(false);
        service.Setup(s => s.UpdateUserAsync(5, It.IsAny<UpdateUserRequest>())).ReturnsAsync(new UserDto { UserId = 5 });
        service.Setup(s => s.UpdateUserAsync(6, It.IsAny<UpdateUserRequest>())).ReturnsAsync((UserDto?)null);
        service.Setup(s => s.DeleteUserAsync(7)).ReturnsAsync(true);
        service.Setup(s => s.DeleteUserAsync(8)).ReturnsAsync(false);

        var controller = CreateUsersController(service, currentUserId: 99);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<NoContentResult>(await controller.UpdateRole(3, new UpdateRoleRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.UpdateRole(4, new UpdateRoleRequest()));
        Assert.IsType<OkObjectResult>(await controller.Update(5, new UpdateUserRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(6, new UpdateUserRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(7));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(8));

        var selfDelete = await CreateUsersController(service, 99).Delete(99);
        Assert.IsType<BadRequestObjectResult>(selfDelete);
        service.Verify(s => s.DeleteUserAsync(99), Times.Never);
    }

    [Fact]
    public async Task UsersController_MapsDuplicateEmailToConflict()
    {
        var service = new Mock<IAuthService>();
        service.Setup(s => s.UpdateUserAsync(1, It.IsAny<UpdateUserRequest>()))
            .ThrowsAsync(new BusinessRuleException("Email already exists."));

        var controller = CreateUsersController(service, 99);
        Assert.IsType<ConflictObjectResult>(await controller.Update(1, new UpdateUserRequest()));
    }

    private static UsersController CreateUsersController(Mock<IAuthService> service, int currentUserId)
    {
        return new UsersController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()) }, "test"))
                }
            }
        };
    }
}
