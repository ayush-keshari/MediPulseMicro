using Xunit;
using LogisticsService.Data;
using LogisticsService.DTOs;
using LogisticsService.Models;
using LogisticsService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace LogisticsService.Tests;

public class TransferOrderStatusTransitionTests
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
    public async Task UpdateTransferStatusAsync_DeductsStock_OnCompletion()
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
        Assert.Equal("Completed", updatedOrder!.Status);

        // Verify stock was deducted from source facility
        var position = await context.InventoryPositions.FirstOrDefaultAsync();
        Assert.Equal(100, position!.Quantity); // 200 - 100 = 100 remaining
    }

    // NEW TESTS FOR TASK #3: Specific status transition tests with InvalidOperationException asserts

    [Fact]
    public async Task UpdateTransferStatusAsync_ThrowsException_WhenInvalidTransition_FromDraftToCompleted()
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
    public async Task UpdateTransferStatusAsync_ThrowsException_WhenInvalidTransition_FromApprovedToDraft()
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

        // Create a transfer order and advance it to Approved status
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

        // Advance to Approved: Draft -> Submitted -> Approved
        await service.UpdateTransferStatusAsync(order.TransferOrderId, new UpdateTransferStatusRequest { Status = "Submitted" });
        await service.UpdateTransferStatusAsync(order.TransferOrderId, new UpdateTransferStatusRequest { Status = "Approved" });

        // Verify it's now Approved
        order = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Equal("Approved", order!.Status);

        // Act & Assert - Trying to go from Approved back to Draft should fail
        var invalidRequest = new UpdateTransferStatusRequest { Status = "Draft" };
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateTransferStatusAsync(order.TransferOrderId, invalidRequest));
    }
}
