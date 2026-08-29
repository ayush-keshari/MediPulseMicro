using Xunit;
using LogisticsService.Data;
using LogisticsService.DTOs;
using LogisticsService.Models;
using LogisticsService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace LogisticsService.Tests;

public class TransferOrderDeletionTests
{
    private LogisticsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<LogisticsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new LogisticsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task DeleteTransferOrderAsync_ThrowsException_WhenNotInDraftOrCancelledStatus()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory positions with sufficient stock
        context.InventoryPositions.AddRange(
            new InventoryPosition
            {
                ItemId = 1,
                LotId = "LOT-001",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Quantity = 100,
                FacilityId = 1,
                StorageZoneId = 1,
                SafetyStock = 50
            }
        );

        await context.SaveChangesAsync();

        var service = new LogisticsServiceImpl(context);

        // Create a transfer order
        var createRequest = new CreateTransferOrderRequest
        {
            FromFacilityId = 1,
            ToFacilityId = 2,
            FromFacilityName = "Facility A",
            ToFacilityName = "Facility B",
            RequestedBy = "test user",
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 10, ToStorageZoneId = 1 }
            }
        };

        await service.CreateTransferOrderAsync(createRequest);
        var order = await context.TransferOrders.FirstOrDefaultAsync();
        Assert.NotNull(order);
        Assert.Equal("Draft", order.Status);

        // Advance the order to Submitted status (not Draft or Cancelled)
        await service.UpdateTransferStatusAsync(order.TransferOrderId, new UpdateTransferStatusRequest { Status = "Submitted" });

        // Verify it's now Submitted
        order = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Equal("Submitted", order!.Status);

        // Act & Assert - Trying to delete a non-Draft/non-Cancelled order should throw InvalidOperationException
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeleteTransferOrderAsync(order.TransferOrderId));
    }

    [Fact]
    public async Task DeleteTransferOrderAsync_Succeeds_WhenInDraftStatus()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory positions with sufficient stock
        context.InventoryPositions.AddRange(
            new InventoryPosition
            {
                ItemId = 1,
                LotId = "LOT-001",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Quantity = 100,
                FacilityId = 1,
                StorageZoneId = 1,
                SafetyStock = 50
            }
        );

        await context.SaveChangesAsync();

        var service = new LogisticsServiceImpl(context);

        // Create a transfer order
        var createRequest = new CreateTransferOrderRequest
        {
            FromFacilityId = 1,
            ToFacilityId = 2,
            FromFacilityName = "Facility A",
            ToFacilityName = "Facility B",
            RequestedBy = "test user",
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 10, ToStorageZoneId = 1 }
            }
        };

        await service.CreateTransferOrderAsync(createRequest);
        var order = await context.TransferOrders.FirstOrDefaultAsync();
        Assert.NotNull(order);
        Assert.Equal("Draft", order.Status);

        // Act
        var result = await service.DeleteTransferOrderAsync(order.TransferOrderId);

        // Assert
        Assert.True(result);

        // Verify the order was deleted
        var deletedOrder = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Null(deletedOrder);
    }

    [Fact]
    public async Task DeleteTransferOrderAsync_Succeeds_WhenInCancelledStatus()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory positions with sufficient stock
        context.InventoryPositions.AddRange(
            new InventoryPosition
            {
                ItemId = 1,
                LotId = "LOT-001",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Quantity = 100,
                FacilityId = 1,
                StorageZoneId = 1,
                SafetyStock = 50
            }
        );

        await context.SaveChangesAsync();

        var service = new LogisticsServiceImpl(context);

        // Create a transfer order
        var createRequest = new CreateTransferOrderRequest
        {
            FromFacilityId = 1,
            ToFacilityId = 2,
            FromFacilityName = "Facility A",
            ToFacilityName = "Facility B",
            RequestedBy = "test user",
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 10, ToStorageZoneId = 1 }
            }
        };

        await service.CreateTransferOrderAsync(createRequest);
        var order = await context.TransferOrders.FirstOrDefaultAsync();
        Assert.NotNull(order);
        Assert.Equal("Draft", order.Status);

        // Cancel the order
        var cancelRequest = new UpdateTransferStatusRequest { Status = "Cancelled" };
        var cancelResult = await service.UpdateTransferStatusAsync(order.TransferOrderId, cancelRequest);
        Assert.True(cancelResult);

        // Verify it's now Cancelled
        order = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Equal("Cancelled", order!.Status);

        // Act
        var result = await service.DeleteTransferOrderAsync(order.TransferOrderId);

        // Assert
        Assert.True(result);

        // Verify the order was deleted
        var deletedOrder = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Null(deletedOrder);
    }
}
