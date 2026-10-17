using AuditService.Controllers;
using AuditService.DTOs;
using AuditService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuditService.Tests;

public class AuditControllerTests
{
    [Fact]
    public async Task Log_DelegatesRequestAndReturnsNoContent()
    {
        var service = new Mock<IAuditLogService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateAuditLogRequest>())).ReturnsAsync(true);
        var controller = new AuditController(service.Object);
        var request = new CreateAuditLogRequest { UserId = "user-1", HttpMethod = "POST", Endpoint = "/api/items" };

        Assert.IsType<NoContentResult>(await controller.Log(request));
        service.Verify(s => s.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task QueryAndGetById_ReturnExpectedResults()
    {
        var service = new Mock<IAuditLogService>();
        service.Setup(s => s.QueryAsync(It.IsAny<AuditQueryParams>()))
            .ReturnsAsync(new PagedResult<AuditLogDto> { Total = 1, Items = [new AuditLogDto { AuditLogId = 2 }] });
        service.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((AuditLogDto?)null);
        service.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(new AuditLogDto { AuditLogId = 2 });
        var controller = new AuditController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.Query(new AuditQueryParams()));
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
    }
}
