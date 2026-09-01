using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.WellBore.Service.Managers;

namespace OSDC.Drilling.WellBore.Service.Controllers;

internal static class WellBoreMutationActionResults
{
    public static ActionResult ToActionResult(this ControllerBase controller, WellBoreMutationResult outcome) => outcome.FailureKind switch
    {
        WellBoreMutationFailureKind.None => controller.Ok(),
        WellBoreMutationFailureKind.InvalidRequest => controller.BadRequest(outcome.Error),
        WellBoreMutationFailureKind.NotFound => controller.NotFound(outcome.Error),
        WellBoreMutationFailureKind.Conflict => controller.Conflict(outcome.Error),
        _ => controller.StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
    };

    public static ActionResult ToActionResult<T>(this ControllerBase controller, WellBoreMutationResult outcome, T? successValue) =>
        outcome.FailureKind == WellBoreMutationFailureKind.None
            ? controller.Ok(successValue)
            : controller.ToActionResult(outcome);
}


