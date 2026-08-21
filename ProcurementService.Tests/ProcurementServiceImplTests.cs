using Xunit;
using ProcurementService.Data;
using ProcurementService.DTOs;
using ProcurementService.Models;
using ProcurementService.Services;
using Microsoft.EntityFrameworkCore;

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
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
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
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
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
}