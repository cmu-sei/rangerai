// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Services;

/// <summary>
/// Turns a broken spec rule into a problem response carrying the rule's code: 409 for a conflict with what others
/// wrote (STALE, ID_TAKEN), 422 for any other rule.
/// </summary>
public sealed class ChronicleExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not ChronicleException e)
            return false;

        context.Response.StatusCode = e.Code is "STALE" or "ID_TAKEN" ? StatusCodes.Status409Conflict : StatusCodes.Status422UnprocessableEntity;
        return await problems.TryWriteAsync(new()
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = "The entry breaks a Chronicle rule.",
                Detail = e.Message,
                Extensions = { ["code"] = e.Code }
            }
        });
    }
}
