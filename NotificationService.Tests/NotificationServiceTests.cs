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
    }
}