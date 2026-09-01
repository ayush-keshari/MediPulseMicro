using InventoryService.Controllers;
using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InventoryService.Tests;

public class ItemsControllerTests
{
    [Fact]
    public async Task GetAllAndGetById_ReturnExpectedResults()
    {
        var service = new Mock<IInventoryService>();
        service.Setup(s => s.GetAllItemsAsync()).ReturnsAsync(Array.Empty<ItemResponse>());
        service.Setup(s => s.GetItemByIdAsync(1)).ReturnsAsync((ItemResponse?)null);
        service.Setup(s => s.GetItemByIdAsync(2)).ReturnsAsync(new ItemResponse { ItemId = 2 });
        var controller = new ItemsController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        var response = Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.Equal(2, Assert.IsType<ItemResponse>(response.Value).ItemId);
    }

    [Fact]
    public async Task Create_ReturnsCreatedOrConflict()
    {
        var service = new Mock<IInventoryService>();
        service.Setup(s => s.CreateItemAsync(It.Is<CreateItemRequest>(r => r.ItemCode == "NEW")))
            .ReturnsAsync((new ItemResponse { ItemId = 5, ItemCode = "NEW" }, (string?)null));
        service.Setup(s => s.CreateItemAsync(It.Is<CreateItemRequest>(r => r.ItemCode == "DUP")))
            .ReturnsAsync(((ItemResponse?)null, "Item code already exists"));
        var controller = new ItemsController(service.Object);

        var created = Assert.IsType<CreatedAtActionResult>(await controller.Create(new CreateItemRequest { ItemCode = "NEW" }));
        Assert.Equal(5, Assert.IsType<ItemResponse>(created.Value).ItemId);
        var conflict = Assert.IsType<ConflictObjectResult>(await controller.Create(new CreateItemRequest { ItemCode = "DUP" }));
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task UpdateAndDelete_ReturnNoContentOrNotFound()
    {
        var service = new Mock<IInventoryService>();
        service.Setup(s => s.UpdateItemAsync(1, It.IsAny<UpdateItemRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateItemAsync(2, It.IsAny<UpdateItemRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteItemAsync(1)).ReturnsAsync(true);
        service.Setup(s => s.DeleteItemAsync(2)).ReturnsAsync(false);
        var controller = new ItemsController(service.Object);

        Assert.IsType<NoContentResult>(await controller.Update(1, new UpdateItemRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(2, new UpdateItemRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(1));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(2));
    }
}
