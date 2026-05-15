using CRM.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Extensions;

public static class ResultExtensions
{
    /// <summary>Converts a service-layer <see cref="Result{T}"/> into an MVC <see cref="ActionResult"/>.</summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller)
        where T : class
    {
        if (result.IsSuccess) return controller.Ok(result.Value);

        return result.ErrorCode switch
        {
            ResultErrorCodes.NotFound => controller.NotFound(BuildProblem(controller, 404, result)),
            ResultErrorCodes.Duplicate => controller.Conflict(BuildProblem(controller, 409, result)),
            ResultErrorCodes.Validation => controller.BadRequest(BuildProblem(controller, 400, result)),
            ResultErrorCodes.Unauthorized => controller.Unauthorized(BuildProblem(controller, 401, result)),
            ResultErrorCodes.Forbidden => new ObjectResult(BuildProblem(controller, 403, result)) { StatusCode = 403 },
            ResultErrorCodes.Conflict => controller.Conflict(BuildProblem(controller, 409, result)),
            _ => new ObjectResult(BuildProblem(controller, 500, result)) { StatusCode = 500 },
        };
    }

    public static IActionResult ToNoContentResult(this Result<bool> result, ControllerBase controller)
    {
        if (result.IsSuccess) return controller.NoContent();

        return result.ErrorCode switch
        {
            ResultErrorCodes.NotFound => controller.NotFound(BuildProblem(controller, 404, result)),
            ResultErrorCodes.Duplicate => controller.Conflict(BuildProblem(controller, 409, result)),
            ResultErrorCodes.Conflict => controller.Conflict(BuildProblem(controller, 409, result)),
            ResultErrorCodes.Validation => controller.BadRequest(BuildProblem(controller, 400, result)),
            _ => new ObjectResult(BuildProblem(controller, 500, result)) { StatusCode = 500 },
        };
    }

    private static ProblemDetails BuildProblem<T>(ControllerBase controller, int status, Result<T> result) => new()
    {
        Title = result.ErrorCode ?? "error",
        Status = status,
        Type = $"https://httpstatuses.io/{status}",
        Instance = controller.HttpContext.Request.Path,
        Detail = result.Error,
    };
}
