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

}
