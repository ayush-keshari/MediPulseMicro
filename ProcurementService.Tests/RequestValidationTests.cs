using System.ComponentModel.DataAnnotations;
using ProcurementService.DTOs;

namespace ProcurementService.Tests;

public class RequestValidationTests
{
    [Fact]
    public void ProcurementRequests_RejectZeroForeignKeys()
    {
        Assert.Contains(Validate(new CreatePurchaseOrderRequest()),
            error => error.ErrorMessage == "SupplierId must be greater than 0.");
        Assert.Contains(Validate(new CreateReceiptRequest()),
            error => error.ErrorMessage == "PoId must be greater than 0.");
    }

    private static IEnumerable<ValidationResult> Validate(object request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);
        return results;
    }
}
