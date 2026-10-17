using LogisticsService.Controllers;
using LogisticsService.DTOs;
using LogisticsService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Exceptions;

namespace LogisticsService.Tests;

public class LogisticsControllerTests
{
    [Fact]
    public async Task ConsumptionController_CoversQueriesAndMutations()
    {
        var service = new Mock<ILogisticsService>();
        service.Setup(s => s.GetAllConsumptionAsync()).ReturnsAsync(Array.Empty<ConsumptionRecordDto>());
        service.Setup(s => s.GetConsumptionByIdAsync(1)).ReturnsAsync((ConsumptionRecordDto?)null);
        service.Setup(s => s.GetConsumptionByIdAsync(2)).ReturnsAsync(new ConsumptionRecordDto { ConsumptionId = 2 });
        service.Setup(s => s.GetConsumptionByFacilityAsync(3)).ReturnsAsync(Array.Empty<ConsumptionRecordDto>());
        service.Setup(s => s.GetConsumptionByItemAsync(4)).ReturnsAsync(Array.Empty<ConsumptionRecordDto>());
        service.Setup(s => s.UpdateConsumptionAsync(5, It.IsAny<UpdateConsumptionRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateConsumptionAsync(6, It.IsAny<UpdateConsumptionRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteConsumptionAsync(7)).ReturnsAsync(true);
        service.Setup(s => s.DeleteConsumptionAsync(8)).ReturnsAsync(false);
        service.Setup(s => s.DeleteConsumptionAsync(9)).ThrowsAsync(new BusinessRuleException("Cannot delete."));

        var controller = new ConsumptionController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<OkObjectResult>(await controller.GetByFacility(3));
        Assert.IsType<OkObjectResult>(await controller.GetByItem(4));
        Assert.IsType<NoContentResult>(await controller.Create(new CreateConsumptionRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(5, new UpdateConsumptionRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(6, new UpdateConsumptionRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(7));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(8));
        Assert.IsType<ConflictObjectResult>(await controller.Delete(9));
    }

    [Fact]
    public async Task TransferOrdersController_CoversQueriesAndMutations()
    {
        var service = new Mock<ILogisticsService>();
        service.Setup(s => s.GetAllTransferOrdersAsync()).ReturnsAsync(Array.Empty<TransferOrderDto>());
        service.Setup(s => s.GetTransferOrderByIdAsync(1)).ReturnsAsync((TransferOrderDto?)null);
        service.Setup(s => s.GetTransferOrderByIdAsync(2)).ReturnsAsync(new TransferOrderDto { TransferOrderId = 2 });
        service.Setup(s => s.GetTransferOrdersByFacilityAsync(3)).ReturnsAsync(Array.Empty<TransferOrderDto>());
        service.Setup(s => s.CreateTransferOrderAsync(It.IsAny<CreateTransferOrderRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateTransferOrderAsync(4, It.IsAny<UpdateTransferOrderRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateTransferOrderAsync(5, It.IsAny<UpdateTransferOrderRequest>())).ReturnsAsync(false);
        service.Setup(s => s.UpdateTransferStatusAsync(6, It.IsAny<UpdateTransferStatusRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateTransferStatusAsync(7, It.IsAny<UpdateTransferStatusRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteTransferOrderAsync(8)).ReturnsAsync(true);
        service.Setup(s => s.DeleteTransferOrderAsync(9)).ReturnsAsync(false);
        service.Setup(s => s.DeleteTransferOrderAsync(10)).ThrowsAsync(new BusinessRuleException("Cannot delete."));

        var controller = new TransferOrdersController(
            service.Object,
            new Mock<IHttpClientFactory>().Object,
            new ConfigurationBuilder().Build())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<OkObjectResult>(await controller.GetByFacility(3));
        Assert.IsType<NoContentResult>(await controller.Create(new CreateTransferOrderRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(4, new UpdateTransferOrderRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(5, new UpdateTransferOrderRequest()));
        Assert.IsType<NoContentResult>(await controller.UpdateStatus(6, new UpdateTransferStatusRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.UpdateStatus(7, new UpdateTransferStatusRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(8));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(9));
        Assert.IsType<ConflictObjectResult>(await controller.Delete(10));
    }
}
