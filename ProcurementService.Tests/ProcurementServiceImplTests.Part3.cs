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
        Assert.NotNull(submittedPo);
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
