// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Chronicle.Api.Models;
using Chronicle.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Controllers.Api;

/// <summary>Chronicles: one record per event. Every read is an audience's view.</summary>
[Route("api/chronicles")]
public sealed class ChroniclesController(ChronicleStore store) : ApiControllerBase
{
    /// <summary>Every Chronicle, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<ChronicleSummary>>(StatusCodes.Status200OK)]
    public IEnumerable<ChronicleSummary> List() => store.All().Select(ChronicleSummary.From);

    /// <summary>One Chronicle, as an audience may see it.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<ChronicleRecord>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<ChronicleRecord> Get(string id, [FromQuery] ViewQuery view) =>
        store.Get(id) is { } record
            ? AudienceView.For(record, view.AudienceOrDefault, view.Viewers)
            : ChronicleNotFound(id);

    /// <summary>Starts a Chronicle, or imports one with the history that made it.</summary>
    [HttpPost]
    [ProducesResponseType<ChronicleRecord>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<InvalidChronicle>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChronicleRecord>> Create([FromBody] ChronicleRecord record, CancellationToken ct)
    {
        var findings = await store.Create(record, ct);
        if (findings.FirstOrDefault(f => f.Code == "CHRONICLE_EXISTS") is { } exists)
            return Problem(StatusCodes.Status409Conflict, exists.Code, exists.Message);
        if (findings.Count > 0)
            return UnprocessableEntity(new InvalidChronicle("INVALID_CHRONICLE", findings));
        return CreatedAtAction(nameof(Get), new { id = record.Id }, store.Get(record.Id));
    }

    /// <summary>What the spec's validation finds in a stored Chronicle. For designers.</summary>
    [HttpGet("{id}/findings")]
    [ProducesResponseType<IReadOnlyList<Finding>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<Finding>> Findings(string id) =>
        store.Get(id) is { } record ? Ok(Validation.Check(record)) : ChronicleNotFound(id);

    /// <summary>Checks a record against the spec and stores nothing.</summary>
    [HttpPost("validate")]
    [ProducesResponseType<IReadOnlyList<Finding>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IReadOnlyList<Finding> Validate([FromBody] ChronicleRecord record) => Validation.Check(record);
}
