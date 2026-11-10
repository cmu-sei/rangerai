// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Controllers.Api;

/// <summary>The spec's JSON Schemas, generated from the Chronicle library, for apps not written in .NET.</summary>
[Route("api/schema")]
public sealed class SchemaController : ApiControllerBase
{
    /// <summary>The JSON Schema for a whole record.</summary>
    [HttpGet("record")]
    [Produces("application/schema+json")]
    [ProducesResponseType<JsonObject>(StatusCodes.Status200OK)]
    public ContentResult Record() => Schema(ChronicleSchema.Record());

    /// <summary>The JSON Schema for one entry, as apps publish it.</summary>
    [HttpGet("entry")]
    [Produces("application/schema+json")]
    [ProducesResponseType<JsonObject>(StatusCodes.Status200OK)]
    public ContentResult Entry() => Schema(ChronicleSchema.Entry());

    private ContentResult Schema(JsonObject schema) => Content(schema.ToJsonString(), "application/schema+json");
}
