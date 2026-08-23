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
        context.InventoryPositions.Add(new InventoryPosition
        {
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
            new InventoryPosition
            {
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
        Assert.Equal("Completed", updatedOrder.Status);

        // Verify stock was deducted from source facility
        var position = await context.InventoryPositions.FirstOrDefaultAsync();
        Assert.Equal(100, position.Quantity); // 200 - 100 = 100 remaining
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
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
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
        Assert.Equal("Approved", order.Status);

        // Act & Assert - Trying to go from Approved back to Draft should fail
        var invalidRequest = new UpdateTransferStatusRequest { Status = "Draft" };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateTransferStatusAsync(order.TransferOrderId, invalidRequest));
    }

    [Fact]
    public async Task UpdateTransferOrderAsync_ThrowsException_WhenNotInDraftStatus()
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

        // Advance the order to Submitted status
        await service.UpdateTransferStatusAsync(order.TransferOrderId, new UpdateTransferStatusRequest { Status = "Submitted" });

        // Verify it's now Submitted
        order = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Equal("Submitted", order.Status);

        // Act & Assert - Trying to update a non-Draft order should throw InvalidOperationException
        var updateRequest = new UpdateTransferOrderRequest
        {
            Items = new List<TransferOrderItemRequest>
            {
                new() { ItemId = 1, ItemName = "Updated Item", Quantity = 20, ToStorageZoneId = 2 }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateTransferOrderAsync(order.TransferOrderId, updateRequest));
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
        Assert.Equal("Submitted", order.Status);

        // Act & Assert - Trying to delete a non-Draft/non-Cancelled order should throw InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
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
        Assert.Equal("Cancelled", order.Status);

        // Act
        var result = await service.DeleteTransferOrderAsync(order.TransferOrderId);

        // Assert
        Assert.True(result);

        // Verify the order was deleted
        var deletedOrder = await context.TransferOrders.FindAsync(order.TransferOrderId);
        Assert.Null(deletedOrder);
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
        Assert.Equal("Completed", updatedOrder.Status);

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
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
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
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTransferOrderAsync(createRequest));
    }
}