using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NotificationService.Controllers;
using NotificationService.DTOs;
using NotificationService.Services;
using Shared.Constants;

namespace NotificationService.Tests;

public class NotificationsControllerTests
{
    private static NotificationsController Create(Mock<INotificationService> service, bool admin = false)
    {
        var claims = new List<Claim> { new("sub", "user-1") };
        if (admin) claims.Add(new(ClaimTypes.Role, Roles.Admin));
        var controller = new NotificationsController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAll_ScopesNonAdminQueryToCaller()
    {
        var service = new Mock<INotificationService>();
        service.Setup(s => s.GetNotificationsAsync(It.Is<NotificationQueryParams>(q => q.UserId == "user-1")))
            .ReturnsAsync(Array.Empty<NotificationDto>());

        Assert.IsType<OkObjectResult>(await Create(service).GetAll(new NotificationQueryParams()));
        service.Verify(s => s.GetNotificationsAsync(It.Is<NotificationQueryParams>(q => q.UserId == "user-1")), Times.Once);
    }

    [Fact]
    public async Task Mutations_ReturnExpectedResults()
    {
        var service = new Mock<INotificationService>();
        service.Setup(s => s.GetUnreadCountAsync("user-1")).ReturnsAsync(3);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateNotificationRequest>())).ReturnsAsync(true);
        service.Setup(s => s.MarkReadAsync(1, "user-1", false)).ReturnsAsync(true);
        service.Setup(s => s.MarkReadAsync(2, "user-1", false)).ReturnsAsync(false);
        service.Setup(s => s.MarkAllReadAsync("user-1")).Returns(Task.CompletedTask);
        service.Setup(s => s.DeleteAsync(1, "user-1", false)).ReturnsAsync(true);
        service.Setup(s => s.DeleteAsync(2, "user-1", false)).ReturnsAsync(false);
        var controller = Create(service);

        Assert.Equal(3, Assert.IsType<UnreadCountDto>((await controller.UnreadCount() as OkObjectResult)!.Value).Count);
        Assert.IsType<NoContentResult>(await controller.Create(new CreateNotificationRequest()));
        Assert.IsType<NoContentResult>(await controller.MarkRead(1));
        Assert.IsType<NotFoundObjectResult>(await controller.MarkRead(2));
        Assert.IsType<NoContentResult>(await controller.MarkAllRead());
        Assert.IsType<NoContentResult>(await controller.Delete(1));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(2));
    }
}
