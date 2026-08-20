using Xunit;
using ProcurementService.Models;

namespace ProcurementService.Tests;

public class ProcurementServiceTests
{
    [Fact]
    public void Supplier_HasName()
    {
        var supplier = new Supplier { Name = "Cipla Limited" };
        Assert.Equal("Cipla Limited", supplier.Name);
    }

    [Fact]
    public void Supplier_HasStatus()
    {
        var supplier = new Supplier { Status = "Active" };
        Assert.Equal("Active", supplier.Status);
    }

    [Fact]
    public void PurchaseOrder_HasStatus()
    {
        var po = new PurchaseOrder { Status = "Draft" };
        Assert.Equal("Draft", po.Status);
    }

    [Fact]
    public void Receipt_HasQualityStatus()
    {
        var receipt = new Receipt { QualityStatus = "Accepted" };
        Assert.Equal("Accepted", receipt.QualityStatus);
    }
}