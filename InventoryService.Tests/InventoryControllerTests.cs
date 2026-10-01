using InventoryService.Controllers;
using InventoryService.DTOs;
using InventoryService.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InventoryService.Tests;

public class InventoryControllerTests
{
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(-1, 1, 1)]
    public void CreatePositionRequest_RejectsNonPositiveIdentifiers(int itemId, int facilityId, int storageZoneId)
    {
        var request = new CreatePositionRequest
        {
            ItemId = itemId,
            LotId = "LOT-1",
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            Quantity = 1,
            FacilityId = facilityId,
            StorageZoneId = storageZoneId,
            SafetyStock = 0
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Any(name =>
            name is nameof(CreatePositionRequest.ItemId)
                or nameof(CreatePositionRequest.FacilityId)
                or nameof(CreatePositionRequest.StorageZoneId)));
    }

    [Fact]
    public async Task GetEndpoints_ReturnOkWithServiceResults()
    {
        var service = new Mock<IInventoryService>();
        service.Setup(s => s.GetAllPositionsAsync()).ReturnsAsync(Array.Empty<PositionResponse>());
        service.Setup(s => s.GetPositionsByItemAsync(3)).ReturnsAsync(Array.Empty<PositionResponse>());
        service.Setup(s => s.GetFacilityIdsByItemAsync(3)).ReturnsAsync(new[] { 4 });
        service.Setup(s => s.GetItemIdsByFacilityAsync(4)).ReturnsAsync(new[] { 3 });
        service.Setup(s => s.GetFacilityStockAsync(4)).ReturnsAsync(Array.Empty<FacilityStockDto>());
        var controller = new InventoryController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<OkObjectResult>(await controller.GetByItem(3));
        Assert.IsType<OkObjectResult>(await controller.GetFacilitiesByItem(3));
        Assert.IsType<OkObjectResult>(await controller.GetItemsByFacility(4));
        Assert.IsType<OkObjectResult>(await controller.GetFacilityStock(4));
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundOrPosition()
    {
        var service = new Mock<IInventoryService>();
        service.Setup(s => s.GetPositionByIdAsync(1)).ReturnsAsync((PositionResponse?)null);
        service.Setup(s => s.GetPositionByIdAsync(2)).ReturnsAsync(new PositionResponse { PositionId = 2 });
        var controller = new InventoryController(service.Object);

        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        var response = Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.Equal(2, Assert.IsType<PositionResponse>(response.Value).PositionId);
    }

    [Fact]
    public async Task MutatingEndpoints_ReturnExpectedStatus()
    {
        var service = new Mock<IInventoryService>();
        service.Setup(s => s.UpdatePositionAsync(1, It.IsAny<UpdatePositionRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdatePositionAsync(2, It.IsAny<UpdatePositionRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeletePositionAsync(1)).ReturnsAsync(true);
        service.Setup(s => s.DeletePositionAsync(2)).ReturnsAsync(false);
        var controller = new InventoryController(service.Object);

        Assert.IsType<NoContentResult>(await controller.Create(new CreatePositionRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(1, new UpdatePositionRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(2, new UpdatePositionRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(1));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(2));
        service.Verify(s => s.CreatePositionAsync(It.IsAny<CreatePositionRequest>()), Times.Once);
    }
}
