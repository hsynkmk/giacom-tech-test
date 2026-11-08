using Microsoft.AspNetCore.Mvc;
using Order.Application.Common;

namespace Order.Api.Extensions;

// Turns a ServiceResult into the matching HTTP response, so every controller action
// maps errors the same way: validation -> 400 ValidationProblem, not found -> 404 ProblemDetails.
public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ServiceResult<T> result,
        ControllerBase controller,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (!result.IsSuccess)
        {
            return controller.ToProblem(result.Error!);
        }

        return onSuccess is null
            ? controller.Ok(result.Value)
            : onSuccess(result.Value);
    }

    public static IActionResult ToActionResult(this ServiceResult result, ControllerBase controller)
    {
        return result.IsSuccess
            ? controller.NoContent()
            : controller.ToProblem(result.Error!);
    }

    private static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        switch (error.Type)
        {
            case ErrorType.Validation:
                // Going through ModelState gives exactly the same response shape
                // as the automatic [ApiController] validation errors.
                foreach (var (field, messages) in error.ValidationErrors)
                {
                    foreach (var message in messages)
                    {
                        controller.ModelState.AddModelError(field, message);
                    }
                }

                return controller.ValidationProblem();

            case ErrorType.NotFound:
                return controller.Problem(detail: error.Message, statusCode: StatusCodes.Status404NotFound);

            default:
                throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type.");
        }
    }
}
