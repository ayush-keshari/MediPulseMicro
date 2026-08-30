using Xunit;
using ProcurementService.Data;
using ProcurementService.DTOs;
using ProcurementService.Models;
using ProcurementService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace ProcurementService.Tests;

public partial class ProcurementServiceImplTests
{
    [Fact]
    public async Task UpdatePoStatusAsync_Succeeds_AllValidStatusTransitions()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // We need a supplier to exist
        var supplierRequest = new CreateSupplierRequest
        {
            Name = "Test Supplier",
            SupplierType = "Manufacturer",
            Status = "Active"
        };
        await service.CreateSupplierAsync(supplierRequest);
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Test Supplier");
        Assert.NotNull(supplier);

        // Test all valid status transitions
        var transitions = new[]
        {
            new { From = "Draft", To = "Submitted" },
            new { From = "Draft", To = "Cancelled" },
            new { From = "Submitted", To = "Approved" },
            new { From = "Submitted", To = "Draft" },
            new { From = "Submitted", To = "Cancelled" },
            new { From = "Approved", To = "Shipped" },
            new { From = "Approved", To = "Cancelled" },
            new { From = "Shipped", To = "PartiallyReceived" },
            new { From = "Shipped", To = "Cancelled" },
            new { From = "PartiallyReceived", To = "FullyReceived" }
        };

        foreach (var transition in transitions)
        {
            // Create a fresh purchase order for each transition test (starts as Draft)
            var expectedOrderDate = DateTime.UtcNow;
            var expectedExpectedDeliveryDate = DateTime.UtcNow.AddDays(7);
            var poRequest = new CreatePurchaseOrderRequest
            {
                SupplierId = supplier.SupplierId,
                OrderDate = expectedOrderDate,
                ExpectedDeliveryDate = expectedExpectedDeliveryDate,
                Notes = $"Test PO for {transition.From} to {transition.To}"
            };

            bool created = await service.CreatePurchaseOrderAsync(poRequest);
            Assert.True(created, "Failed to create purchase order");
            var po = await context.PurchaseOrders.FirstOrDefaultAsync(po => po.Notes == poRequest.Notes);
            Assert.NotNull(po);

            // If the transition doesn't start from Draft, we need to transition to the From state first
            if (transition.From != "Draft")
            {
                // Determine the path from Draft to the From state
                var pathToFrom = GetPathFromDraftToState(transition.From);

                // Apply each transition in the path
                foreach (var state in pathToFrom)
                {
                    var setupRequest = new UpdatePoStatusRequest { Status = state };
                    var setupResult = await service.UpdatePoStatusAsync(po.PoId, setupRequest);
                    Assert.True(setupResult, $"Failed to transition PO to {state} state during setup");

                    // Verify the setup worked
                    var setupPo = await context.PurchaseOrders.FirstOrDefaultAsync(p => p.PoId == po.PoId);
                    Assert.NotNull(setupPo);
                    Assert.Equal(setupPo.Status, state);

                    // Use this PO for the next transition in the path
                    po = setupPo;
                }
            }
            else
            {
                // Verify starting state is Draft
                Assert.Equal(transition.From, po.Status); // Verify starting state
            }

            // Act
            var statusRequest = new UpdatePoStatusRequest { Status = transition.To };
            var result = await service.UpdatePoStatusAsync(po.PoId, statusRequest);

            // Assert
            Assert.True(result, $"Failed to transition from {transition.From} to {transition.To}");

            // Verify status was updated - use a fresh query to avoid context caching issues
            var updatedPo = await context.PurchaseOrders.FirstOrDefaultAsync(p => p.PoId == po.PoId);
            Assert.NotNull(updatedPo);
            Assert.Equal(transition.To, updatedPo.Status);
        }
    }

    // ENHANCED TESTS FOR UpdatePoStatusAsync - INVALID STATUS TRANSITIONS

    [Fact]
    public async Task UpdatePoStatusAsync_ThrowsException_AllInvalidStatusTransitions()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // We need a supplier to exist
        var supplierRequest = new CreateSupplierRequest
        {
            Name = "Test Supplier",
            SupplierType = "Manufacturer",
            Status = "Active"
        };
        await service.CreateSupplierAsync(supplierRequest);
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Test Supplier");
        Assert.NotNull(supplier);

        // Define invalid transitions that should throw exceptions
        var invalidTransitions = new[]
        {
            // From Draft
            new { From = "Draft", To = "Approved" }, // Cannot skip Submitted
            new { From = "Draft", To = "Shipped" }, // Cannot skip Submitted->Approved
            new { From = "Draft", To = "PartiallyReceived" }, // Cannot skip multiple steps
            new { From = "Draft", To = "FullyReceived" }, // Cannot skip multiple steps

            // From Submitted
            new { From = "Submitted", To = "Shipped" }, // Cannot skip Approved
            new { From = "Submitted", To = "PartiallyReceived" }, // Cannot skip Approved->Shipped
            new { From = "Submitted", To = "FullyReceived" }, // Cannot skip multiple steps

            // From Approved
            new { From = "Approved", To = "PartiallyReceived" }, // Cannot skip Shipped
            new { From = "Approved", To = "FullyReceived" }, // Cannot skip Shipped->PartiallyReceived
            new { From = "Approved", To = "Draft" }, // Cannot go backwards more than one step

            // From Shipped
            new { From = "Shipped", To = "FullyReceived" }, // Cannot skip PartiallyReceived
            new { From = "Shipped", To = "Approved" }, // Cannot go backwards
            new { From = "Shipped", To = "Draft" }, // Cannot go backwards multiple steps

            // From PartiallyReceived
            new { From = "PartiallyReceived", To = "Shipped" }, // Cannot go backwards
            new { From = "PartiallyReceived", To = "Approved" }, // Cannot go backwards multiple steps
            new { From = "PartiallyReceived", To = "Draft" }, // Cannot go backwards multiple steps

            // From FullyReceived - cannot transition to any state
            new { From = "FullyReceived", To = "Draft" },
            new { From = "FullyReceived", To = "Submitted" },
            new { From = "FullyReceived", To = "Approved" },
            new { From = "FullyReceived", To = "Shipped" },
            new { From = "FullyReceived", To = "PartiallyReceived" },
            new { From = "FullyReceived", To = "Cancelled" } // Special case: FullyReceived cannot be cancelled
        };

        foreach (var transition in invalidTransitions)
        {
            // Create a fresh purchase order for each transition test (starts as Draft)
            var poRequest = new CreatePurchaseOrderRequest
            {
                SupplierId = supplier.SupplierId,
                OrderDate = DateTime.UtcNow,
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
                Notes = $"Test PO for invalid {transition.From} to {transition.To}"
            };

            bool created = await service.CreatePurchaseOrderAsync(poRequest);
            Assert.True(created, "Failed to create purchase order");
            var po = await context.PurchaseOrders.FirstOrDefaultAsync(po => po.Notes == poRequest.Notes);
            Assert.NotNull(po);

            // If the transition doesn't start from Draft, we need to transition to the From state first
            if (transition.From != "Draft")
            {
                // Determine the path from Draft to the From state
                var pathToFrom = GetPathFromDraftToState(transition.From);

                // Apply each transition in the path
                foreach (var state in pathToFrom)
                {
                    var setupRequest = new UpdatePoStatusRequest { Status = state };
                    var setupResult = await service.UpdatePoStatusAsync(po.PoId, setupRequest);
                    Assert.True(setupResult, $"Failed to transition PO to {state} state during setup");

                    // Verify the setup worked
                    var setupPo = await context.PurchaseOrders.FirstOrDefaultAsync(p => p.PoId == po.PoId);
                    Assert.NotNull(setupPo);
                    Assert.Equal(setupPo.Status, state);

                    // Use this PO for the next transition in the path
                    po = setupPo;
                }
            }
            else
            {
                // Verify starting state is Draft
                Assert.Equal(transition.From, po.Status); // Verify starting state
            }

            // Act & Assert
            var statusRequest = new UpdatePoStatusRequest { Status = transition.To };
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                service.UpdatePoStatusAsync(po.PoId, statusRequest));
        }
    }

    // ENHANCED TESTS FOR MISSING ENTITIES

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Act
        var result = await service.GetPurchaseOrderByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPurchaseOrdersBySupplierAsync_ReturnsEmptyList_WhenSupplierHasNoOrders()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Create a supplier with no purchase orders
        var supplierRequest = new CreateSupplierRequest
        {
            Name = "Test Supplier",
            SupplierType = "Manufacturer",
            Status = "Active"
        };
        await service.CreateSupplierAsync(supplierRequest);
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Test Supplier");
        Assert.NotNull(supplier);

        // Act
        var result = await service.GetPurchaseOrdersBySupplierAsync(supplier.SupplierId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ENHANCED TESTS FOR UpdatePurchaseOrderAsync

    [Fact]
    public async Task UpdatePurchaseOrderAsync_Succeeds_WhenOrderIsInDraftStatus()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // We need a supplier to exist
        var supplierRequest = new CreateSupplierRequest
        {
            Name = "Test Supplier",
            SupplierType = "Manufacturer",
            Status = "Active"
        };
        await service.CreateSupplierAsync(supplierRequest);
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Test Supplier");
        Assert.NotNull(supplier);

        // Create a purchase order in Draft status
        var poRequest = new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Notes = "Original notes"
        };
        await service.CreatePurchaseOrderAsync(poRequest);
        var po = await context.PurchaseOrders.FirstOrDefaultAsync();
        Assert.NotNull(po);
        Assert.Equal("Draft", po.Status);

        // Calculate expected dates
        var expectedOrderDate = DateTime.UtcNow.AddDays(1);
        var expectedExpectedDeliveryDate = DateTime.UtcNow.AddDays(10);

        // Update request
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = expectedOrderDate,
            ExpectedDeliveryDate = expectedExpectedDeliveryDate,
            Notes = "Updated notes"
        };

        // Act
        var result = await service.UpdatePurchaseOrderAsync(po.PoId, updateRequest);

        // Assert
        Assert.True(result);

        // Verify purchase order was updated
        var updatedPo = await context.PurchaseOrders.FindAsync(po.PoId);
        Assert.NotNull(updatedPo);
        Assert.Equal(expectedOrderDate, updatedPo.OrderDate);
        Assert.Equal(expectedExpectedDeliveryDate, updatedPo.ExpectedDeliveryDate);
        Assert.Equal("Updated notes", updatedPo.Notes);
        Assert.Equal("Draft", updatedPo.Status); // Status should remain unchanged
    }

}
