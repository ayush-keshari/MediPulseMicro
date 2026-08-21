using Xunit;
using LogisticsService.Data;
using LogisticsService.DTOs;
using LogisticsService.Models;
using LogisticsService.Services;
using Microsoft.EntityFrameworkCore;

namespace LogisticsService.Tests;

public class LogisticsServiceImplTests
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
    public async Task CreateTransferOrderAsync_ThrowsException_WhenSourceAndDestinationAreSame()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new LogisticsServiceImpl(context);

        var request = new CreateTransferOrderRequest
        {
            FromFacilityId = 1,
            ToFacilityId = 1, // Same as source
            FromFacilityName = "Facility A",
            ToFacilityName = "Facility A",
            RequestedBy = "test user",
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 10, ToStorageZoneId = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTransferOrderAsync(request));
    }

    [Fact]
    public async Task CreateTransferOrderAsync_ThrowsException_WhenInsufficientStock()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory position with insufficient stock (less than requested quantity)
        context.InventoryPositions.Add(new InventoryPosition {
            ItemId = 1,
            LotId = "LOT-001",
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Quantity = 50, // Only 50 available, requesting 100
            FacilityId = 1,
            StorageZoneId = 1,
            SafetyStock = 10
        });

        await context.SaveChangesAsync();

        var service = new LogisticsServiceImpl(context);

        var request = new CreateTransferOrderRequest
        {
            FromFacilityId = 1,
            ToFacilityId = 2,
            FromFacilityName = "Facility A",
            ToFacilityName = "Facility B",
            RequestedBy = "test user",
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 100, ToStorageZoneId = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTransferOrderAsync(request));
    }

    [Fact]
    public async Task CreateTransferOrderAsync_Succeeds_WhenSufficientStockExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory positions with sufficient stock
        context.InventoryPositions.AddRange(
            new InventoryPosition {
                ItemId = 1,
                LotId = "LOT-001",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Quantity = 200, // Enough for our request of 100
                FacilityId = 1,
                StorageZoneId = 1,
                SafetyStock = 50
            }
        );

        await context.SaveChangesAsync();

        var service = new LogisticsServiceImpl(context);

        var request = new CreateTransferOrderRequest
        {
            FromFacilityId = 1,
            ToFacilityId = 2,
            FromFacilityName = "Facility A",
            ToFacilityName = "Facility B",
            RequestedBy = "test user",
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 100, ToStorageZoneId = 1 }
            }
        };

        // Act
        var result = await service.CreateTransferOrderAsync(request);

        // Assert
        Assert.True(result);

        // Verify the transfer order was created
        var order = await context.TransferOrders.FirstOrDefaultAsync();
        Assert.NotNull(order);
        Assert.Equal("Draft", order.Status);
        Assert.Single(order.Items);
        Assert.Equal(1, order.Items.First().ItemId);
        Assert.Equal(100, order.Items.First().Quantity);

        // Verify stock was NOT deducted yet (only happens on completion)
        var position = await context.InventoryPositions.FirstOrDefaultAsync();
        Assert.Equal(200, position.Quantity); // Should still be 200
    }

    [Fact]
    public async Task UpdateTransferStatusAsync_DeductsStock_OnCompletion()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Add inventory positions with sufficient stock
        context.InventoryPositions.AddRange(
            new InventoryPosition {
                ItemId = 1,
                LotId = "LOT-001",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Quantity = 200,
                FacilityId = 1,
                StorageZoneId = 1,
                SafetyStock = 50
            }
        );

        await context.SaveChangesAsync();

        var service = new LogisticsServiceImpl(context);

        // Create a transfer order first
        var createRequest = new CreateTransferOrderRequest
        {
            FromFacilityId = 1,
            ToFacilityId = 2,
            FromFacilityName = "Facility A",
            ToFacilityName = "Facility B",
            RequestedBy = "test user",
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Test Item", Quantity = 100, ToStorageZoneId = 1 }
            }
        };

        await service.CreateTransferOrderAsync(createRequest);

        // Get the created order
        var order = await context.TransferOrders.FirstOrDefaultAsync();
        Assert.NotNull(order);

        // Act - Complete the transfer order by going through all status transitions
        // Draft -> Submitted
        var submitRequest = new UpdateTransferStatusRequest { Status = "Submitted" };
        var submitResult = await service.UpdateTransferStatusAsync(order.TransferOrderId, submitRequest);
        Assert.True(submitResult);

        // Submitted -> Approved
        var approveRequest = new UpdateTransferStatusRequest { Status = "Approved" };
        var approveResult = await service.UpdateTransferStatusAsync(order.TransferOrderId, approveRequest);
        Assert.True(approveResult);

        // Approved -> InTransit
        var inTransitRequest = new UpdateTransferStatusRequest { Status = "InTransit" };
        var inTransitResult = await service.UpdateTransferStatusAsync(order.TransferOrderId, inTransitRequest);
        Assert.True(inTransitResult);

        // InTransit -> Completed
        var completeRequest = new UpdateTransferStatusRequest { Status = "Completed" };
        var result = await service.UpdateTransferStatusAsync(order.TransferOrderId, completeRequest);

        // Assert
        Assert.True(result);

        // Verify order status is completed
        var updatedOrder = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Equal("Completed", updatedOrder.Status);

        // Verify stock was deducted from source facility
        var position = await context.InventoryPositions.FirstOrDefaultAsync();
        Assert.Equal(100, position.Quantity); // 200 - 100 = 100 remaining
    }
}