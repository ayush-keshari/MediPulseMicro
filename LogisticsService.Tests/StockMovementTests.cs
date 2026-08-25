using Xunit;
using LogisticsService.Data;
using LogisticsService.DTOs;
using LogisticsService.Models;
using LogisticsService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace LogisticsService.Tests;

public class StockMovementTests
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

    // NEW TESTS FOR MoveStockAsync method (tested through UpdateTransferStatusAsync)

    [Fact]
    public async Task UpdateTransferStatusAsync_MovesStockSuccessfully_OnCompletion()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory positions with sufficient stock at source facility
        context.InventoryPositions.AddRange(
            new InventoryPosition
            {
                ItemId = 1,
                LotId = "LOT-001",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Quantity = 100,
                FacilityId = 1,
                StorageZoneId = 1,
                SafetyStock = 10
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
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 50, ToStorageZoneId = 2 }
            }
        };

        await service.CreateTransferOrderAsync(createRequest);
        var order = await context.TransferOrders.FirstOrDefaultAsync();
        Assert.NotNull(order);
        Assert.Equal("Draft", order.Status);

        // Advance through statuses to Completed
        await service.UpdateTransferStatusAsync(order.TransferOrderId, new UpdateTransferStatusRequest { Status = "Submitted" });
        await service.UpdateTransferStatusAsync(order.TransferOrderId, new UpdateTransferStatusRequest { Status = "Approved" });
        await service.UpdateTransferStatusAsync(order.TransferOrderId, new UpdateTransferStatusRequest { Status = "InTransit" });

        // Act - Complete the transfer (this triggers MoveStockAsync)
        var completeRequest = new UpdateTransferStatusRequest { Status = "Completed" };
        var result = await service.UpdateTransferStatusAsync(order.TransferOrderId, completeRequest);

        // Assert
        Assert.True(result);

        // Verify order status is completed
        var updatedOrder = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Equal("Completed", updatedOrder!.Status);

        // Verify stock was deducted from source facility (FEFO)
        var sourcePosition = await context.InventoryPositions
            .FirstOrDefaultAsync(p => p.ItemId == 1 && p.FacilityId == 1 && p.StorageZoneId == 1);
        Assert.NotNull(sourcePosition);
        Assert.Equal(50, sourcePosition.Quantity); // 100 - 50 = 50 remaining

        // Verify stock was added to destination facility
        var destPosition = await context.InventoryPositions
            .FirstOrDefaultAsync(p => p.ItemId == 1 && p.FacilityId == 2 && p.StorageZoneId == 2);
        Assert.NotNull(destPosition);
        Assert.Equal(50, destPosition.Quantity); // 0 + 50 = 50 added
        Assert.Equal($"XFER-{order.TransferOrderId}", destPosition.LotId);
    }

    [Fact]
    public async Task UpdateTransferStatusAsync_ThrowsException_WhenInvalidTransition_ToCompleted()
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
                SafetyStock = 10
            }
        );

        await context.SaveChangesAsync();

        var service = new LogisticsServiceImpl(context);

        // Create a transfer order in Draft status
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

        // Act & Assert - Trying to go from Draft directly to Completed should fail
        var invalidRequest = new UpdateTransferStatusRequest { Status = "Completed" };
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateTransferStatusAsync(order.TransferOrderId, invalidRequest));
    }

    [Fact]
    public async Task UpdateTransferStatusAsync_ThrowsException_WhenInsufficientStock_ForMoveStock()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory positions with INSUFFICIENT stock at source facility (only 20, requesting 50)
        context.InventoryPositions.AddRange(
            new InventoryPosition
            {
                ItemId = 1,
                LotId = "LOT-001",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Quantity = 20, // Not enough!
                FacilityId = 1,
                StorageZoneId = 1,
                SafetyStock = 10
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
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 50, ToStorageZoneId = 1 } // Requesting 50
            }
        };

        // Act & Assert - Should throw InvalidOperationException during creation due to insufficient stock
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateTransferOrderAsync(createRequest));
    }
}