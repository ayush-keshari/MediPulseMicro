using Microsoft.EntityFrameworkCore;
using ProcurementService.Data;
using ProcurementService.DTOs;
using ProcurementService.Models;
using ProcurementService.Services;
using Shared.Exceptions;

namespace ProcurementService.Tests;

public class PurchaseOrderLifecycleTests
{
    private static readonly string[] States =
        ["Draft", "Submitted", "Approved", "Shipped", "PartiallyReceived", "FullyReceived", "Cancelled"];

    public static IEnumerable<object[]> Transitions()
    {
        var allowed = new HashSet<(string, string)>
        {
            ("Draft", "Submitted"), ("Draft", "Cancelled"),
            ("Submitted", "Approved"), ("Submitted", "Draft"), ("Submitted", "Cancelled"),
            ("Approved", "Shipped"), ("Approved", "Cancelled"),
            ("Shipped", "PartiallyReceived"), ("Shipped", "Cancelled"),
            ("PartiallyReceived", "FullyReceived"), ("PartiallyReceived", "Cancelled")
        };
        foreach (var current in States)
            foreach (var next in States)
                yield return [current, next, allowed.Contains((current, next))];
    }

    private static ProcurementDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ProcurementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Theory]
    [MemberData(nameof(Transitions))]
    public async Task StatusTransitions_PersistOnlyAllowedChanges(string current, string next, bool allowed)
    {
        await using var db = CreateContext();
        var order = new PurchaseOrder { SupplierId = 1, Status = current, Notes = "Preserve notes" };
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();
        var service = new ProcurementServiceImpl(db);

        if (allowed)
            Assert.True(await service.UpdatePoStatusAsync(order.PoId, new() { Status = next }));
        else
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                service.UpdatePoStatusAsync(order.PoId, new() { Status = next }));

        db.ChangeTracker.Clear();
        var saved = await db.PurchaseOrders.SingleAsync();
        Assert.Equal(allowed ? next : current, saved.Status);
        Assert.Equal("Preserve notes", saved.Notes);
    }

    [Theory]
    [InlineData("Unknown", "Cancelled")]
    [InlineData("Draft", "Unknown")]
    [InlineData("Draft", "submitted")]
    public async Task UnknownStatuses_AreRejectedWithoutMutation(string current, string next)
    {
        await using var db = CreateContext();
        var order = new PurchaseOrder { SupplierId = 1, Status = current };
        db.Add(order);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new ProcurementServiceImpl(db).UpdatePoStatusAsync(order.PoId, new() { Status = next }));
        db.ChangeTracker.Clear();
        Assert.Equal(current, (await db.PurchaseOrders.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("Approved", true)]
    [InlineData("Shipped", true)]
    [InlineData("PartiallyReceived", true)]
    [InlineData("Draft", false)]
    [InlineData("Submitted", false)]
    [InlineData("FullyReceived", false)]
    [InlineData("Cancelled", false)]
    public async Task ReceiptCreation_RespectsOrderLifecycle(string status, bool allowed)
    {
        await using var db = CreateContext();
        var order = new PurchaseOrder { SupplierId = 1, Status = status };
        db.Add(order);
        await db.SaveChangesAsync();
        var request = new CreateReceiptRequest
        {
            PoId = order.PoId,
            QuantityReceived = 7,
            SupplierLot = "LOT-7",
            ReceivedBy = "warehouse",
            QualityStatus = "Accepted"
        };
        var service = new ProcurementServiceImpl(db);
        if (allowed)
            Assert.True(await service.CreateReceiptAsync(request));
        else
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateReceiptAsync(request));

        db.ChangeTracker.Clear();
        Assert.Equal(allowed ? "PartiallyReceived" : status, (await db.PurchaseOrders.SingleAsync()).Status);
        if (allowed)
        {
            var receipt = await db.Receipts.SingleAsync();
            Assert.Equal(order.PoId, receipt.PoId);
            Assert.Equal(7, receipt.QuantityReceived);
            Assert.Equal("LOT-7", receipt.SupplierLot);
        }
        else
            Assert.Empty(await db.Receipts.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deletion_RemovesOwnedGraphButPreservesOtherSupplier(bool deleteSupplier)
    {
        await using var db = CreateContext();
        var supplier = new Supplier { Name = "Owner", SupplierType = "Manufacturer", Status = "Active" };
        var other = new Supplier { Name = "Other", SupplierType = "Manufacturer", Status = "Active" };
        var selected = new PurchaseOrder { Supplier = supplier };
        var sibling = new PurchaseOrder { Supplier = supplier };
        var unrelated = new PurchaseOrder { Supplier = other };
        db.AddRange(selected, sibling, unrelated);
        db.Receipts.AddRange(new Receipt { PurchaseOrder = selected, QuantityReceived = 1 },
            new Receipt { PurchaseOrder = sibling, QuantityReceived = 2 },
            new Receipt { PurchaseOrder = unrelated, QuantityReceived = 3 });
        await db.SaveChangesAsync();
        var service = new ProcurementServiceImpl(db);

        Assert.True(deleteSupplier
            ? await service.DeleteSupplierAsync(supplier.SupplierId)
            : await service.DeletePurchaseOrderAsync(selected.PoId));

        db.ChangeTracker.Clear();
        Assert.Null(await db.PurchaseOrders.FindAsync(selected.PoId));
        Assert.Equal(deleteSupplier ? 1 : 2, await db.PurchaseOrders.CountAsync());
        Assert.Equal(deleteSupplier ? 1 : 2, await db.Receipts.CountAsync());
        Assert.NotNull(await db.Suppliers.FindAsync(other.SupplierId));
        Assert.Equal(3, (await db.Receipts.SingleAsync(r => r.PoId == unrelated.PoId)).QuantityReceived);
    }
}
