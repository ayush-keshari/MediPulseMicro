using InventoryService.Controllers;
using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace InventoryService.Tests;

public class ExceptionControllerTests
{
    private static ExceptionsController Create(Mock<IExceptionService> service)
    {
        var controller = new ExceptionsController(service.Object, new Mock<IHttpClientFactory>().Object,
            new ConfigurationBuilder().Build());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Fact]
    public async Task GetAndGetById_ReturnExpectedResults()
    {
        var service = new Mock<IExceptionService>();
        service.Setup(s => s.GetAllAsync(null, null, null)).ReturnsAsync(Array.Empty<ExceptionEventDto>());
        service.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((ExceptionEventDto?)null);
        service.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(new ExceptionEventDto { ExceptionId = 2 });
        var controller = Create(service);

        Assert.IsType<OkObjectResult>(await controller.GetAll(null, null, null));
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
    }

    [Fact]
    public async Task Mutations_ReturnNoContentOrNotFound()
    {
        var service = new Mock<IExceptionService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateExceptionRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateStatusAsync(1, It.IsAny<UpdateExceptionStatusRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateStatusAsync(2, It.IsAny<UpdateExceptionStatusRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
        service.Setup(s => s.DeleteAsync(2)).ReturnsAsync(false);
        service.Setup(s => s.DetectAsync(1, 7)).ReturnsAsync(new DetectExceptionsResult { TotalCreated = 0 });
        var controller = Create(service);

        Assert.IsType<NoContentResult>(await controller.Create(new CreateExceptionRequest()));
        Assert.IsType<NoContentResult>(await controller.UpdateStatus(1, new UpdateExceptionStatusRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.UpdateStatus(2, new UpdateExceptionStatusRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(1));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(2));
        Assert.IsType<OkObjectResult>(await controller.Detect(1, 7));
    }
}

public class ReplenishmentControllerTests
{
    private static ReplenishmentController Create(Mock<IReplenishmentService> service)
    {
        var controller = new ReplenishmentController(service.Object, new Mock<IHttpClientFactory>().Object,
            new ConfigurationBuilder().Build());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Fact]
    public async Task Queries_ReturnOkOrNotFound()
    {
        var service = new Mock<IReplenishmentService>();
        service.Setup(s => s.GetForecastsAsync(1, 2)).ReturnsAsync(Array.Empty<ForecastDto>());
        service.Setup(s => s.GetForecastByIdAsync(1)).ReturnsAsync((ForecastDto?)null);
        service.Setup(s => s.GetPlansAsync(1, "Pending", "High")).ReturnsAsync(Array.Empty<ReplenishmentPlanDto>());
        service.Setup(s => s.GetPlanByIdAsync(1)).ReturnsAsync((ReplenishmentPlanDto?)null);
        var controller = Create(service);

        Assert.IsType<OkObjectResult>(await controller.GetForecasts(1, 2));
        Assert.IsType<NotFoundObjectResult>(await controller.GetForecastById(1));
        Assert.IsType<OkObjectResult>(await controller.GetPlans(1, "Pending", "High"));
        Assert.IsType<NotFoundObjectResult>(await controller.GetPlanById(1));
    }

    [Fact]
    public async Task PlanMutations_ReturnExpectedResults()
    {
        var service = new Mock<IReplenishmentService>();
        service.Setup(s => s.UpdatePlanStatusAsync(1, It.IsAny<UpdatePlanStatusRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdatePlanStatusAsync(2, It.IsAny<UpdatePlanStatusRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeletePlanAsync(1)).ReturnsAsync(true);
        service.Setup(s => s.DeletePlanAsync(2)).ReturnsAsync(false);
        service.Setup(s => s.GenerateAsync(3)).ReturnsAsync(new GenerateReplenishmentResult { FacilityId = 3 });
        var controller = Create(service);

        Assert.IsType<NoContentResult>(await controller.UpdatePlanStatus(1, new UpdatePlanStatusRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.UpdatePlanStatus(2, new UpdatePlanStatusRequest()));
        Assert.IsType<NoContentResult>(await controller.DeletePlan(1));
        Assert.IsType<NotFoundObjectResult>(await controller.DeletePlan(2));
        Assert.IsType<OkObjectResult>(await controller.Generate(3));
    }
}

public class RecallActionsControllerTests
{
    [Fact]
    public async Task QueryAndCreate_ReturnExpectedResults()
    {
        var service = new Mock<IExceptionService>();
        service.Setup(s => s.GetActionsAsync(1)).ReturnsAsync(Array.Empty<RecallActionDto>());
        service.Setup(s => s.GetActionByIdAsync(1)).ReturnsAsync((RecallActionDto?)null);
        service.Setup(s => s.CreateActionAsync(It.IsAny<CreateRecallActionRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateActionAsync(1, It.IsAny<UpdateRecallActionRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateActionAsync(2, It.IsAny<UpdateRecallActionRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteActionAsync(1)).ReturnsAsync(true);
        service.Setup(s => s.DeleteActionAsync(2)).ReturnsAsync(false);
        var controller = new RecallActionsController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetByException(1));
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<NoContentResult>(await controller.Create(new CreateRecallActionRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(1, new UpdateRecallActionRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(2, new UpdateRecallActionRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(1));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(2));
    }
}
