using FacilityService.Controllers;
using FacilityService.DTOs;
using FacilityService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Exceptions;

namespace FacilityService.Tests;

public class FacilityControllerTests
{
    [Fact]
    public async Task FacilitiesController_CoversQueriesAndMutations()
    {
        var service = new Mock<IFacilityService>();
        service.Setup(s => s.GetAllFacilitiesAsync()).ReturnsAsync(Array.Empty<FacilityDto>());
        service.Setup(s => s.GetFacilityByIdAsync(1)).ReturnsAsync((FacilityDto?)null);
        service.Setup(s => s.GetFacilityByIdAsync(2)).ReturnsAsync(new FacilityDto { FacilityId = 2 });
        service.Setup(s => s.GetZonesByFacilityAsync(2)).ReturnsAsync(Array.Empty<StorageZoneDto>());
        service.Setup(s => s.CreateFacilityAsync(It.IsAny<CreateFacilityRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateFacilityAsync(3, It.IsAny<UpdateFacilityRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateFacilityAsync(4, It.IsAny<UpdateFacilityRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteFacilityAsync(5)).ReturnsAsync(true);
        service.Setup(s => s.DeleteFacilityAsync(6)).ReturnsAsync(false);
        service.Setup(s => s.CreateFacilityAsync(It.Is<CreateFacilityRequest>(_ => true)))
            .ReturnsAsync(true);

        var controller = new FacilitiesController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<OkObjectResult>(await controller.GetZones(2));
        Assert.IsType<NoContentResult>(await controller.Create(new CreateFacilityRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(3, new UpdateFacilityRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(4, new UpdateFacilityRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(5));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(6));
    }

    [Fact]
    public async Task FacilitiesController_MapsCreateAndUpdateConflicts()
    {
        var service = new Mock<IFacilityService>();
        service.Setup(s => s.CreateFacilityAsync(It.IsAny<CreateFacilityRequest>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate facility."));
        service.Setup(s => s.UpdateFacilityAsync(1, It.IsAny<UpdateFacilityRequest>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate facility."));
        var controller = new FacilitiesController(service.Object);

        Assert.IsType<ConflictObjectResult>(await controller.Create(new CreateFacilityRequest()));
        Assert.IsType<ConflictObjectResult>(await controller.Update(1, new UpdateFacilityRequest()));
    }

    [Fact]
    public async Task StorageZonesController_CoversQueriesAndMutations()
    {
        var service = new Mock<IFacilityService>();
        service.Setup(s => s.GetAllZonesAsync()).ReturnsAsync(Array.Empty<StorageZoneDto>());
        service.Setup(s => s.GetZoneByIdAsync(1)).ReturnsAsync((StorageZoneDto?)null);
        service.Setup(s => s.GetZoneByIdAsync(2)).ReturnsAsync(new StorageZoneDto { ZoneId = 2 });
        service.Setup(s => s.CreateZoneAsync(It.IsAny<CreateStorageZoneRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateZoneAsync(3, It.IsAny<UpdateStorageZoneRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateZoneAsync(4, It.IsAny<UpdateStorageZoneRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteZoneAsync(5)).ReturnsAsync(true);
        service.Setup(s => s.DeleteZoneAsync(6)).ReturnsAsync(false);
        service.Setup(s => s.CreateZoneAsync(It.IsAny<CreateStorageZoneRequest>()))
            .ThrowsAsync(new BusinessRuleException("Duplicate zone."));

        var controller = new StorageZonesController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<ConflictObjectResult>(await controller.Create(new CreateStorageZoneRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(3, new UpdateStorageZoneRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(4, new UpdateStorageZoneRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(5));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(6));
    }
}
