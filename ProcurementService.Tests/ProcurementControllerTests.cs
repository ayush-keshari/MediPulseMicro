using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using ProcurementService.Controllers;
using ProcurementService.DTOs;
using ProcurementService.Services;
using Shared.Exceptions;

namespace ProcurementService.Tests;

public class ProcurementControllerTests
{
    [Fact]
    public async Task PurchaseOrdersController_CoversQueriesAndLifecycle()
    {
        var service = new Mock<IProcurementService>();
        service.Setup(s => s.GetAllPurchaseOrdersAsync()).ReturnsAsync(Array.Empty<PurchaseOrderDto>());
        service.Setup(s => s.GetPurchaseOrderByIdAsync(1)).ReturnsAsync((PurchaseOrderDto?)null);
        service.Setup(s => s.GetPurchaseOrderByIdAsync(2)).ReturnsAsync(new PurchaseOrderDto { PoId = 2 });
        service.Setup(s => s.GetReceiptsByPoAsync(2)).ReturnsAsync(Array.Empty<ReceiptDto>());
        service.Setup(s => s.CreatePurchaseOrderAsync(It.IsAny<CreatePurchaseOrderRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdatePurchaseOrderAsync(3, It.IsAny<UpdatePurchaseOrderRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdatePurchaseOrderAsync(4, It.IsAny<UpdatePurchaseOrderRequest>())).ReturnsAsync(false);
        service.Setup(s => s.UpdatePoStatusAsync(5, It.IsAny<UpdatePoStatusRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdatePoStatusAsync(6, It.IsAny<UpdatePoStatusRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeletePurchaseOrderAsync(7)).ReturnsAsync(true);
        service.Setup(s => s.DeletePurchaseOrderAsync(8)).ReturnsAsync(false);

        var controller = new PurchaseOrdersController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<NotFoundObjectResult>(await controller.GetReceipts(1));
        Assert.IsType<OkObjectResult>(await controller.GetReceipts(2));
        Assert.IsType<NoContentResult>(await controller.Create(new CreatePurchaseOrderRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(3, new UpdatePurchaseOrderRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(4, new UpdatePurchaseOrderRequest()));
        Assert.IsType<NoContentResult>(await controller.UpdateStatus(5, new UpdatePoStatusRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.UpdateStatus(6, new UpdatePoStatusRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(7));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(8));
    }

    [Fact]
    public async Task PurchaseOrdersController_MapsBusinessRuleConflicts()
    {
        var service = new Mock<IProcurementService>();
        service.Setup(s => s.CreatePurchaseOrderAsync(It.IsAny<CreatePurchaseOrderRequest>()))
            .ThrowsAsync(new BusinessRuleException("Invalid supplier."));
        service.Setup(s => s.UpdatePurchaseOrderAsync(1, It.IsAny<UpdatePurchaseOrderRequest>()))
            .ThrowsAsync(new BusinessRuleException("Order is not editable."));
        service.Setup(s => s.UpdatePoStatusAsync(1, It.IsAny<UpdatePoStatusRequest>()))
            .ThrowsAsync(new BusinessRuleException("Invalid transition."));
        service.Setup(s => s.DeletePurchaseOrderAsync(1))
            .ThrowsAsync(new BusinessRuleException("Order has receipts."));
        var controller = new PurchaseOrdersController(service.Object);

        Assert.IsType<ConflictObjectResult>(await controller.Create(new CreatePurchaseOrderRequest()));
        Assert.IsType<ConflictObjectResult>(await controller.Update(1, new UpdatePurchaseOrderRequest()));
        Assert.IsType<ConflictObjectResult>(await controller.UpdateStatus(1, new UpdatePoStatusRequest()));
        Assert.IsType<ConflictObjectResult>(await controller.Delete(1));
    }

    [Fact]
    public async Task SuppliersController_CoversQueriesAndMutations()
    {
        var service = new Mock<IProcurementService>();
        service.Setup(s => s.GetAllSuppliersAsync()).ReturnsAsync(Array.Empty<SupplierDto>());
        service.Setup(s => s.GetSupplierByIdAsync(1)).ReturnsAsync((SupplierDto?)null);
        service.Setup(s => s.GetSupplierByIdAsync(2)).ReturnsAsync(new SupplierDto { SupplierId = 2 });
        service.Setup(s => s.GetPurchaseOrdersBySupplierAsync(2)).ReturnsAsync(Array.Empty<PurchaseOrderDto>());
        service.Setup(s => s.CreateSupplierAsync(It.IsAny<CreateSupplierRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateSupplierAsync(3, It.IsAny<UpdateSupplierRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateSupplierAsync(4, It.IsAny<UpdateSupplierRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteSupplierAsync(5)).ReturnsAsync(true);
        service.Setup(s => s.DeleteSupplierAsync(6)).ReturnsAsync(false);
        var controller = new SuppliersController(service.Object);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<NotFoundObjectResult>(await controller.GetPurchaseOrders(1));
        Assert.IsType<OkObjectResult>(await controller.GetPurchaseOrders(2));
        Assert.IsType<NoContentResult>(await controller.Create(new CreateSupplierRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(3, new UpdateSupplierRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(4, new UpdateSupplierRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(5));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(6));
    }

    [Fact]
    public async Task ReceiptsController_CoversQueriesAndValidationResponses()
    {
        var service = new Mock<IProcurementService>();
        service.Setup(s => s.GetAllReceiptsAsync()).ReturnsAsync(Array.Empty<ReceiptDto>());
        service.Setup(s => s.GetReceiptByIdAsync(1)).ReturnsAsync((ReceiptDto?)null);
        service.Setup(s => s.GetReceiptByIdAsync(2)).ReturnsAsync(new ReceiptDto { ReceiptId = 2 });
        service.Setup(s => s.CreateReceiptAsync(It.IsAny<CreateReceiptRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateReceiptAsync(3, It.IsAny<UpdateReceiptRequest>())).ReturnsAsync(true);
        service.Setup(s => s.UpdateReceiptAsync(4, It.IsAny<UpdateReceiptRequest>())).ReturnsAsync(false);
        service.Setup(s => s.DeleteReceiptAsync(5)).ReturnsAsync(true);
        service.Setup(s => s.DeleteReceiptAsync(6)).ReturnsAsync(false);
        var controller = new ReceiptsController(
            service.Object,
            new Mock<IHttpClientFactory>().Object,
            new ConfigurationBuilder().Build())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsType<OkObjectResult>(await controller.GetById(2));
        Assert.IsType<NoContentResult>(await controller.Create(new CreateReceiptRequest()));
        Assert.IsType<NoContentResult>(await controller.Update(3, new UpdateReceiptRequest()));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(4, new UpdateReceiptRequest()));
        Assert.IsType<NoContentResult>(await controller.Delete(5));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(6));
    }

    [Fact]
    public async Task ReceiptsController_MapsCreateBusinessRuleToUnprocessableEntity()
    {
        var service = new Mock<IProcurementService>();
        service.Setup(s => s.CreateReceiptAsync(It.IsAny<CreateReceiptRequest>()))
            .ThrowsAsync(new BusinessRuleException("PO is not receivable."));
        var controller = new ReceiptsController(
            service.Object,
            new Mock<IHttpClientFactory>().Object,
            new ConfigurationBuilder().Build())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        Assert.IsType<UnprocessableEntityObjectResult>(await controller.Create(new CreateReceiptRequest()));
    }
}
