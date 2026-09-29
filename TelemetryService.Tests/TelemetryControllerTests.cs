using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Exceptions;
using TelemetryService.Controllers;
using TelemetryService.DTOs;
using TelemetryService.Services;

namespace TelemetryService.Tests;

public class TelemetryControllerTests
{
    [Fact]
    public async Task SensorEndpoints_ReturnExpectedResults()
    {
        var service = new Mock<ITelemetryService>();
        service.Setup(s => s.GetAllSensorsAsync()).ReturnsAsync(Array.Empty<SensorDeviceDto>());
        service.Setup(s => s.GetSensorByIdAsync(1)).ReturnsAsync((SensorDeviceDto?)null);
        service.Setup(s => s.GetSensorByIdAsync(2)).ReturnsAsync(new SensorDeviceDto { SensorId = 2 });
        service.Setup(s => s.GetTelemetryBySensorAsync(2)).ReturnsAsync(Array.Empty<TelemetryRecordDto>());
        var controller = new SensorDevicesController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<NotFoundObjectResult>(await controller.GetTelemetry(1));
        Assert.IsType<OkObjectResult>(await controller.GetTelemetry(2));
    }

    [Fact]
    public async Task TelemetryMutations_HandleSuccessNotFoundAndConflict()
    {
        var service = new Mock<ITelemetryService>();
        service.Setup(s => s.CreateSensorAsync(It.IsAny<CreateSensorDeviceRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateSensorAsync(1, It.IsAny<UpdateSensorDeviceRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateSensorAsync(2, It.IsAny<UpdateSensorDeviceRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteSensorAsync(1)).ReturnsAsync(true);
        service.Setup(s => s.DeleteSensorAsync(2)).ReturnsAsync(false);
        service.Setup(s => s.DeleteSensorAsync(3)).ThrowsAsync(new BusinessRuleException("Telemetry exists."));
        var controller = new SensorDevicesController(service.Object);

        Assert.IsType<NoContentResult>(await controller.Create(new CreateSensorDeviceRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(1, new UpdateSensorDeviceRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(2, new UpdateSensorDeviceRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(1));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(2));
        Assert.IsType<ConflictObjectResult>(await controller.Delete(3));
    }

    [Fact]
    public async Task TelemetryRecordEndpoints_ReturnExpectedResults()
    {
        var service = new Mock<ITelemetryService>();
        service.Setup(s => s.GetAllTelemetryAsync()).ReturnsAsync(Array.Empty<TelemetryRecordDto>());
        service.Setup(s => s.GetExcursionsAsync()).ReturnsAsync(Array.Empty<TelemetryRecordDto>());
        service.Setup(s => s.GetTelemetryByIdAsync(1)).ReturnsAsync((TelemetryRecordDto?)null);
        service.Setup(s => s.CreateTelemetryAsync(It.IsAny<CreateTelemetryRecordRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateTelemetryAsync(1, It.IsAny<UpdateTelemetryRecordRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateTelemetryAsync(2, It.IsAny<UpdateTelemetryRecordRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteTelemetryAsync(1)).ReturnsAsync(true);
        service.Setup(s => s.DeleteTelemetryAsync(2)).ReturnsAsync(false);
        service.Setup(s => s.CreateTelemetryAsync(It.Is<CreateTelemetryRecordRequest>(r => r.SensorId == 9)))
            .ThrowsAsync(new BusinessRuleException("Sensor not found."));
        var controller = new TelemetryRecordsController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<OkObjectResult>(await controller.GetExcursions());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<NoContentResult>(await controller.Create(new CreateTelemetryRecordRequest { SensorId = 1 }));
        Assert.IsType<ConflictObjectResult>(await controller.Create(new CreateTelemetryRecordRequest { SensorId = 9 }));
        Assert.IsType<NoContentResult>(await controller.Update(1, new UpdateTelemetryRecordRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(2, new UpdateTelemetryRecordRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(1));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(2));
    }
}
