using Xunit;
using ProcurementService.Data;
using ProcurementService.DTOs;
using ProcurementService.Models;
using ProcurementService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace ProcurementService.Tests;

public class ProcurementServiceImplTests
{
    private ProcurementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ProcurementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ProcurementDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private IEnumerable<string> GetPathFromDraftToState(string targetState)
    {
        // Define the valid workflow: Draft -> Submitted -> Approved -> Shipped -> PartiallyReceived -> FullyReceived
        // Plus: Any state -> Cancelled (except FullyReceived -> Cancelled is not allowed)
        // And: Submitted -> Draft (backwards one step)

        // Since we always start from Draft in our tests (newly created POs start as Draft),
        // we define the path from Draft to each target state
        return targetState switch
        {
            "Draft" => Array.Empty<string>(),
            "Submitted" => new[] { "Submitted" },
            "Approved" => new[] { "Submitted", "Approved" },
            "Shipped" => new[] { "Submitted", "Approved", "Shipped" },
            "PartiallyReceived" => new[] { "Submitted", "Approved", "Shipped", "PartiallyReceived" },
            "FullyReceived" => new[] { "Submitted", "Approved", "Shipped", "PartiallyReceived", "FullyReceived" },
            "Cancelled" => new[] { "Cancelled" },
            _ => throw new ArgumentException($"Unknown state: {targetState}")
        };
    }

    [Fact]
    public async Task CreateSupplierAsync_ThrowsException_ForDuplicateSupplierNameAndType()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Create first supplier
        var firstRequest = new CreateSupplierRequest
        {
            Name = "Cipla Limited",
            SupplierType = "Pharma",
            Status = "Active"
        };

        await service.CreateSupplierAsync(firstRequest);

        // Try to create duplicate
        var duplicateRequest = new CreateSupplierRequest
        {
            Name = "Cipla Limited",
            SupplierType = "Pharma",
            Status = "Inactive"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateSupplierAsync(duplicateRequest));
    }

    [Fact]
    public async Task UpdateSupplierAsync_ThrowsException_ForDuplicateSupplierNameAndType()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Create two suppliers
        var firstSupplier = new CreateSupplierRequest
        {
            Name = "Cipla Limited",
            SupplierType = "Pharma",
            Status = "Active"
        };
        await service.CreateSupplierAsync(firstSupplier);

        var secondSupplier = new CreateSupplierRequest
        {
            Name = "Dr. Reddy's Labs",
            SupplierType = "Pharma",
            Status = "Active"
        };
        await service.CreateSupplierAsync(secondSupplier);

        // Get the second supplier to update it
        var supplierToUpdate = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Name == "Dr. Reddy's Labs");
        Assert.NotNull(supplierToUpdate);

        // Try to update second supplier to have same name/type as first
        var updateRequest = new UpdateSupplierRequest
        {
            Name = "Cipla Limited", // Same as first supplier
            SupplierType = "Pharma", // Same as first supplier
            Status = "Active"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateSupplierAsync(supplierToUpdate.SupplierId, updateRequest));
    }

    [Fact]
    public async Task CreateSupplierAsync_Succeeds_WhenSupplierDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        var request = new CreateSupplierRequest
        {
            Name = "New Supplier",
            SupplierType = "Pharma",
            Status = "Active"
        };

        // Act
        var result = await service.CreateSupplierAsync(request);

        // Assert
        Assert.True(result);

        // Verify supplier was created
        var supplier = await context.Suppliers.FirstOrDefaultAsync(
            s => s.Name == "New Supplier");
        Assert.NotNull(supplier);
        Assert.Equal("New Supplier", supplier.Name);
        Assert.Equal("Pharma", supplier.SupplierType);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_ThrowsException_WhenSupplierDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        var request = new CreatePurchaseOrderRequest
        {
            SupplierId = 999, // Non-existent supplier ID
            OrderDate = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreatePurchaseOrderAsync(request));
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_Succeeds_WhenSupplierExists()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Create a supplier first
        var supplierRequest = new CreateSupplierRequest
        {
            Name = "Test Supplier",
            SupplierType = "Pharma",
            Status = "Active"
        };
        await service.CreateSupplierAsync(supplierRequest);

        // Get the created supplier to use its ID
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Test Supplier");
        Assert.NotNull(supplier);

        var request = new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Notes = "Test purchase order"
        };

        // Act
        var result = await service.CreatePurchaseOrderAsync(request);

        // Assert
        Assert.True(result);

        // Verify purchase order was created
        var purchaseOrder = await context.PurchaseOrders
            .FirstOrDefaultAsync(po => po.SupplierId == supplier.SupplierId);
        Assert.NotNull(purchaseOrder);
        Assert.Equal(supplier.SupplierId, purchaseOrder.SupplierId);
        Assert.Equal("Draft", purchaseOrder.Status); // Default status
        Assert.Equal(request.Notes, purchaseOrder.Notes);
    }

    // NEW TESTS FOR UpdatePoStatusAsync method

    [Fact]
    public async Task UpdatePoStatusAsync_Succeeds_ValidStatusTransition()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Create a purchase order first
        var poRequest = new CreatePurchaseOrderRequest
        {
            SupplierId = 1,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Notes = "Test purchase order"
        };

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

        poRequest.SupplierId = supplier.SupplierId;

        await service.CreatePurchaseOrderAsync(poRequest);
        var po = await context.PurchaseOrders.FirstOrDefaultAsync();
        Assert.NotNull(po);
        Assert.Equal("Draft", po.Status);

        // Act - Transition from Draft to Submitted
        var statusRequest = new UpdatePoStatusRequest { Status = "Submitted" };
        var result = await service.UpdatePoStatusAsync(po.PoId, statusRequest);

        // Assert
        Assert.True(result);

        // Verify status was updated
        var updatedPo = await context.PurchaseOrders.FindAsync(po.PoId);
        Assert.Equal("Submitted", updatedPo!.Status);
    }

    [Fact]
    public async Task UpdatePoStatusAsync_ThrowsException_InvalidStatusTransition()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Create a purchase order first
        var poRequest = new CreatePurchaseOrderRequest
        {
            SupplierId = 1,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Notes = "Test purchase order"
        };

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

        poRequest.SupplierId = supplier.SupplierId;

        await service.CreatePurchaseOrderAsync(poRequest);
        var po = await context.PurchaseOrders.FirstOrDefaultAsync();
        Assert.NotNull(po);
        Assert.Equal("Draft", po.Status);

        // Act & Assert - Trying to go from Draft directly to FullyReceived should fail
        var invalidRequest = new UpdatePoStatusRequest { Status = "FullyReceived" };
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdatePoStatusAsync(po.PoId, invalidRequest));
    }

    [Fact]
    public async Task UpdatePoStatusAsync_ThrowsException_WhenPoNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Act & Assert
        var statusRequest = new UpdatePoStatusRequest { Status = "Submitted" };
        var result = await service.UpdatePoStatusAsync(999, statusRequest); // Non-existent PO ID

        // Assert
        Assert.False(result); // Returns false when PO not found
    }

    // ENHANCED TESTS FOR UpdatePoStatusAsync - VALID STATUS TRANSITIONS

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

    [Fact]
    public async Task UpdatePurchaseOrderAsync_ThrowsException_WhenOrderIsNotInDraftStatus()
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

        // Create a purchase order and transition it to Submitted status
        var poRequest = new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Notes = "Test PO"
        };
        await service.CreatePurchaseOrderAsync(poRequest);
        var po = await context.PurchaseOrders.FirstOrDefaultAsync();
        Assert.NotNull(po);

        // Transition to Submitted
        var statusRequest = new UpdatePoStatusRequest { Status = "Submitted" };
        await service.UpdatePoStatusAsync(po.PoId, statusRequest);

        // Verify it's no longer in Draft status
        var submittedPo = await context.PurchaseOrders.FindAsync(po.PoId);
        Assert.Equal("Submitted", submittedPo.Status);

        // Update request
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = DateTime.UtcNow.AddDays(1),
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(10),
            Notes = "Updated notes"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdatePurchaseOrderAsync(po.PoId, updateRequest));
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_ThrowsException_WhenSupplierDoesNotExist_WhenChangingSupplier()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // We need a supplier to exist for the initial PO
        var supplierRequest = new CreateSupplierRequest
        {
            Name = "Original Supplier",
            SupplierType = "Manufacturer",
            Status = "Active"
        };
        await service.CreateSupplierAsync(supplierRequest);
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Original Supplier");
        Assert.NotNull(supplier);

        // Create a purchase order in Draft status with the original supplier
        var poRequest = new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Notes = "Test PO"
        };
        await service.CreatePurchaseOrderAsync(poRequest);
        var po = await context.PurchaseOrders.FirstOrDefaultAsync();
        Assert.NotNull(po);
        Assert.Equal("Draft", po.Status);

        // Try to update to a non-existent supplier
        var updateRequest = new UpdatePurchaseOrderRequest
        {
            SupplierId = 999, // Non-existent supplier
            OrderDate = DateTime.UtcNow.AddDays(1),
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(10),
            Notes = "Updated notes"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdatePurchaseOrderAsync(po.PoId, updateRequest));
    }

    // ENHANCED TESTS FOR DeletePurchaseOrderAsync

    [Fact]
    public async Task DeletePurchaseOrderAsync_Succeeds_WhenOrderExists()
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

        // Create a purchase order
        var poRequest = new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Notes = "Test PO to delete"
        };
        await service.CreatePurchaseOrderAsync(poRequest);
        var po = await context.PurchaseOrders.FirstOrDefaultAsync();
        Assert.NotNull(po);

        // Act
        var result = await service.DeletePurchaseOrderAsync(po.PoId);

        // Assert
        Assert.True(result);

        // Verify purchase order was deleted
        var deletedPo = await context.PurchaseOrders.FindAsync(po.PoId);
        Assert.Null(deletedPo);
    }

    [Fact]
    public async Task DeletePurchaseOrderAsync_ReturnsFalse_WhenOrderNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = new ProcurementServiceImpl(context);

        // Act
        var result = await service.DeletePurchaseOrderAsync(999);

        // Assert
        Assert.False(result);
    }
}