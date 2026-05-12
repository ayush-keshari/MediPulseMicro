using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.Filters;

// Runs BEFORE every controller action.
// Checks all DTO data annotations ([Required], [EmailAddress], [MinLength] etc.)
// Returns a consistent JSON 400 if any validation fails — same format across all services.
// Registered globally via AddMediPulseControllers() extension method.
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new BadRequestObjectResult(new
            {
                message = "Validation failed.",
                errors
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
