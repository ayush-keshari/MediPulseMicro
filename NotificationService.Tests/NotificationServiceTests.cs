using Xunit;
using NotificationService.Models;
using NotificationService.Services;
using NotificationService.DTOs;
using NotificationService.Data;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Tests
{
    public class NotificationServiceTests
    {
        private NotificationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new NotificationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task CreateAsync_CreatesNotificationSuccessfully()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var service = new NotificationServiceImpl(context);

            var request = new CreateNotificationRequest
            {
                UserId = "user123",
                Category = "Exception",
                Title = "Low Stock Alert",
                Message = "Item MED-001 is running low"
            };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            Assert.True(result);

            // Verify notification was created
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Title == "Low Stock Alert");
            Assert.NotNull(notification);
            Assert.Equal("user123", notification.UserId);
            Assert.Equal("Exception", notification.Category);
            Assert.Equal("Low Stock Alert", notification.Title);
            Assert.Equal("Item MED-001 is running low", notification.Message);
            Assert.False(notification.IsRead); // Default value
        }

        [Fact]
        public async Task GetNotificationsAsync_AppliesFiltersAndPagination()
        {
            await using var context = CreateInMemoryDbContext();
            context.Notifications.AddRange(
                new Notification { UserId = "user123", Category = "Exception", Title = "Older", Message = "1", CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
                new Notification { UserId = "user123", Category = "Expiry", Title = "Newest", Message = "2", CreatedAt = DateTime.UtcNow },
                new Notification { UserId = "other", Category = "Exception", Title = "Other user", Message = "3", CreatedAt = DateTime.UtcNow.AddMinutes(-1) });
            await context.SaveChangesAsync();

            var service = new NotificationServiceImpl(context);
            var result = await service.GetNotificationsAsync(new NotificationQueryParams
            {
                UserId = "user123",
                IsRead = false,
                Page = 1,
                PageSize = 1
            });

            var notification = Assert.Single(result);
            Assert.Equal("Newest", notification.Title);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsOnlyUnreadNotificationsForUser()
        {
            await using var context = CreateInMemoryDbContext();
            context.Notifications.AddRange(
                new Notification { UserId = "user123", Title = "Unread", IsRead = false },
                new Notification { UserId = "user123", Title = "Read", IsRead = true },
                new Notification { UserId = "other", Title = "Other", IsRead = false });
            await context.SaveChangesAsync();

            var service = new NotificationServiceImpl(context);

            Assert.Equal(1, await service.GetUnreadCountAsync("user123"));
        }

        [Fact]
        public async Task MarkReadAsync_RejectsNonOwner_ButAllowsAdmin()
        {
            await using var context = CreateInMemoryDbContext();
            var notification = new Notification { UserId = "owner", Title = "Alert", IsRead = false };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            var service = new NotificationServiceImpl(context);

            Assert.False(await service.MarkReadAsync(notification.NotificationId, "another-user", false));
            Assert.True(await service.MarkReadAsync(notification.NotificationId, "admin", true));
            Assert.True((await context.Notifications.FindAsync(notification.NotificationId))!.IsRead);
        }

        [Fact]
        public async Task MarkAllReadAsync_MarksOnlyUsersUnreadNotifications()
        {
            await using var context = CreateInMemoryDbContext();
            context.Notifications.AddRange(
                new Notification { UserId = "user123", Title = "One", IsRead = false },
                new Notification { UserId = "user123", Title = "Two", IsRead = true },
                new Notification { UserId = "other", Title = "Other", IsRead = false });
            await context.SaveChangesAsync();
            var service = new NotificationServiceImpl(context);

            await service.MarkAllReadAsync("user123");

            Assert.All(context.Notifications.Where(n => n.UserId == "user123"), n => Assert.True(n.IsRead));
            Assert.False((await context.Notifications.SingleAsync(n => n.UserId == "other")).IsRead);
        }

        [Fact]
        public async Task DeleteAsync_RejectsNonOwner_AndDeletesAsOwner()
        {
            await using var context = CreateInMemoryDbContext();
            var notification = new Notification { UserId = "owner", Title = "Alert" };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            var service = new NotificationServiceImpl(context);

            Assert.False(await service.DeleteAsync(notification.NotificationId, "another-user", false));
            Assert.True(await service.DeleteAsync(notification.NotificationId, "owner", false));
            Assert.Null(await context.Notifications.FindAsync(notification.NotificationId));
        }
    }
}
