using Xunit;
using LogisticsService.Models;

namespace LogisticsService.Tests;

public class LogisticsServiceTests
{
    [Fact]
    public void TransferOrder_HasStatus()
    {
        var order = new TransferOrder { Status = "Draft" };
        Assert.Equal("Draft", order.Status);
    }

    [Fact]
    public void TransferOrderItem_HasQuantity()
    {
        var item = new TransferOrderItem { Quantity = 100 };
        Assert.Equal(100, item.Quantity);
    }

    [Fact]
    public void ConsumptionRecord_HasQuantityConsumed()
    {
        var record = new ConsumptionRecord { QuantityConsumed = 50 };
        Assert.Equal(50, record.QuantityConsumed);
    }
}