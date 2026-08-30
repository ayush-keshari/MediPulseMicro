using Microsoft.EntityFrameworkCore;
using ProcurementService.Data;
using ProcurementService.DTOs;
using ProcurementService.Services;
using Shared.Exceptions;

namespace ProcurementService.Tests;

public class ReceiptBehaviorTests
{
    private static ProcurementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProcurementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ProcurementDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<(ProcurementDbContext Context, ProcurementServiceImpl Service, int PurchaseOrderId)> CreateApprovedOrderAsync()
    {
        var context = CreateContext();
        var service = new ProcurementServiceImpl(context);
        await service.CreateSupplierAsync(new CreateSupplierRequest
        {
            Name = "Test Supplier",
            SupplierType = "Manufacturer",
            Status = "Active"
        });
        var supplier = await context.Suppliers.SingleAsync();
        await service.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest
        {
            SupplierId = supplier.SupplierId,
            OrderDate = new DateTime(2026, 1, 1),
            ExpectedDeliveryDate = new DateTime(2026, 1, 8),
            Notes = "Test order"
        });
        var order = await context.PurchaseOrders.SingleAsync();
        await service.UpdatePoStatusAsync(order.PoId, new UpdatePoStatusRequest { Status = "Submitted" });
        await service.UpdatePoStatusAsync(order.PoId, new UpdatePoStatusRequest { Status = "Approved" });
        return (context, service, order.PoId);
    }

    [Fact]
    public async Task CreateReceiptAsync_ChangesApprovedOrderToPartiallyReceived()
    {
        var setup = await CreateApprovedOrderAsync();
        await using var context = setup.Context;

        var result = await setup.Service.CreateReceiptAsync(new CreateReceiptRequest
        {
            PoId = setup.PurchaseOrderId,
            SupplierLot = "LOT-1",
            ReceivedDate = new DateTime(2026, 1, 5),
            ReceivedBy = "warehouse-1",
            QualityStatus = "Accepted",
            QuantityReceived = 100
        });

        Assert.True(result);
        Assert.Equal("PartiallyReceived", (await context.PurchaseOrders.FindAsync(setup.PurchaseOrderId))!.Status);
        Assert.Equal("Test Supplier", (await setup.Service.GetAllReceiptsAsync()).Single().SupplierName);
    }

    [Fact]
    public async Task CreateReceiptAsync_RejectsMissingOrDraftOrder()
    {
        await using var context = CreateContext();
        var service = new ProcurementServiceImpl(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateReceiptAsync(new CreateReceiptRequest
        {
            PoId = 999,
            ReceivedBy = "warehouse-1",
            QualityStatus = "Accepted",
            QuantityReceived = 1
        }));

        await service.CreateSupplierAsync(new CreateSupplierRequest
        {
            Name = "Draft Supplier",
            SupplierType = "Distributor",
            Status = "Active"
        });
        var supplier = await context.Suppliers.SingleAsync();
        await service.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest { SupplierId = supplier.SupplierId });
        var order = await context.PurchaseOrders.SingleAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateReceiptAsync(new CreateReceiptRequest
        {
            PoId = order.PoId,
            ReceivedBy = "warehouse-1",
            QualityStatus = "Rejected",
            QuantityReceived = 1
        }));
    }

    [Fact]
    public async Task ReceiptCrudAndQueries_ReturnExpectedResults()
    {
        var setup = await CreateApprovedOrderAsync();
        await using var context = setup.Context;
        await setup.Service.CreateReceiptAsync(new CreateReceiptRequest
        {
            PoId = setup.PurchaseOrderId,
            SupplierLot = "LOT-1",
            ReceivedBy = "warehouse-1",
            QualityStatus = "OnHold",
            QuantityReceived = 10
        });
        var receipt = await context.Receipts.SingleAsync();

        Assert.Equal(receipt.ReceiptId, (await setup.Service.GetReceiptByIdAsync(receipt.ReceiptId))!.ReceiptId);
        Assert.Single(await setup.Service.GetReceiptsByPoAsync(setup.PurchaseOrderId));
        Assert.Empty(await setup.Service.GetReceiptsByPoAsync(999));

        Assert.True(await setup.Service.UpdateReceiptAsync(receipt.ReceiptId, new UpdateReceiptRequest
        {
            SupplierLot = "LOT-2",
            ReceivedBy = "warehouse-2",
            QualityStatus = "Accepted",
            QuantityReceived = 20
        }));
        Assert.Equal("LOT-2", (await setup.Service.GetReceiptByIdAsync(receipt.ReceiptId))!.SupplierLot);
        Assert.True(await setup.Service.DeleteReceiptAsync(receipt.ReceiptId));
        Assert.False(await setup.Service.DeleteReceiptAsync(999));
        Assert.Null(await setup.Service.GetReceiptByIdAsync(999));
    }
}
