// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Controllers.Api;

/// <summary>What every API controller shares: JSON in and out, and problem responses that carry a code.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>A problem response, with <paramref name="code"/> for clients to switch on.</summary>
    protected ObjectResult Problem(int status, string code, string detail) =>
        new(new ProblemDetails { Status = status, Title = code, Detail = detail, Extensions = { ["code"] = code } })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };

    /// <summary>404: there is no such Chronicle.</summary>
    protected ObjectResult ChronicleNotFound(string id) =>
        Problem(StatusCodes.Status404NotFound, "NOT_FOUND", $"There is no Chronicle '{id}'.");
}
