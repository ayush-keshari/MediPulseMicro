using Xunit;
using InventoryService.Models;

namespace InventoryService.Tests;

public class InventoryServiceTests
{
    [Fact]
    public void Item_HasItemCode()
    {
        var item = new Item { ItemCode = "MED-001" };
        Assert.Equal("MED-001", item.ItemCode);
    }

    [Fact]
    public void Item_HasCategory()
    {
        var item = new Item { Category = "Pharma" };
        Assert.Equal("Pharma", item.Category);
    }

    [Fact]
    public void InventoryPosition_HasLotId()
    {
        var position = new InventoryPosition { LotId = "LOT-001" };
        Assert.Equal("LOT-001", position.LotId);
    }

    [Fact]
    public void ExceptionEvent_HasType()
    {
        var exception = new ExceptionEvent { Type = "Stockout" };
        Assert.Equal("Stockout", exception.Type);
    }
}