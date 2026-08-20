using Xunit;
using NotificationService.Models;

namespace NotificationService.Tests;

public class NotificationServiceTests
{
    [Fact]
    public void Notification_HasCategory()
    {
        var notif = new Notification { Category = "Exception" };
        Assert.Equal("Exception", notif.Category);
    }

    [Fact]
    public void Notification_HasTitle()
    {
        var notif = new Notification { Title = "Stockout Alert" };
        Assert.Equal("Stockout Alert", notif.Title);
    }

    [Fact]
    public void Notification_CanBeRead()
    {
        var notif = new Notification { IsRead = true };
        Assert.True(notif.IsRead);
    }
}