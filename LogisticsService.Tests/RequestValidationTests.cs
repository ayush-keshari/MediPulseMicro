using System.ComponentModel.DataAnnotations;
using LogisticsService.DTOs;

namespace LogisticsService.Tests;

public class RequestValidationTests
{
    [Fact]
    public void TransferRequests_RejectZeroFacilitiesAndItems()
    {
        var request = new CreateTransferOrderRequest
        {
            FromFacilityId = 0,
            ToFacilityId = 0,
            Items = new List<TransferOrderItemRequest> { new() { ItemId = 0, Quantity = 1, ToStorageZoneId = 1 } }
        };

        var errors = Validate(request).ToList();

        Assert.Contains(errors, error => error.ErrorMessage == "FromFacilityId must be greater than 0.");
        Assert.Contains(errors, error => error.ErrorMessage == "ToFacilityId must be greater than 0.");
        Assert.Contains(Validate(request.Items[0]), error => error.ErrorMessage == "ItemId must be greater than 0.");
    }

    [Fact]
    public void ConsumptionRequests_RejectZeroForeignKeys()
    {
        var request = new CreateConsumptionRequest { FacilityId = 0, ItemId = 0, QuantityConsumed = 1 };

        var errors = Validate(request).ToList();

        Assert.Contains(errors, error => error.ErrorMessage == "FacilityId must be greater than 0.");
        Assert.Contains(errors, error => error.ErrorMessage == "ItemId must be greater than 0.");
    }

    private static IEnumerable<ValidationResult> Validate(object request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);
        return results;
    }
}
