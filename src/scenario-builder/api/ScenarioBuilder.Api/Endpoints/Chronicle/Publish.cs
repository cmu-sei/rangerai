// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Microsoft.EntityFrameworkCore;
using ScenarioBuilder.Api.Domain;
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Endpoints.Chronicle;

public record ChroniclePartCounts(int World, int Intent);

/// <summary>What the Finish page shows after a publish.</summary>
public record ChroniclePublished(string ChronicleId, int Entries, ChroniclePartCounts Parts, int Subscribers, string Url);

/// <summary>
/// POST /scenarios/{id}/chronicle — publishes the scenario's World and Intent to its Chronicle (id: the scenario's
/// slug). The first publish creates it; each one after publishes only what changed. The owner or an admin, never the agent.
/// </summary>
public class PublishEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapPost("/scenarios/{id:int}/chronicle", async (int id, HttpContext http, IConfiguration config, ScenarioTypeCatalog scenarioTypes, BuilderDb db,
                ChroniclePublisher publisher, CancellationToken ct) =>
            {
                var state = await ScenarioState.Load(db, scenarioTypes, id, ct);
                if (state is null)
                    return ScenarioState.Missing(id);
                var caller = Caller.From(http);
                if (caller.IsAgent)
                    return ErrorResults.Forbidden("AGENT_CANNOT_PUBLISH", "Only a person publishes the scenario to its Chronicle, on the Finish page.");
                if (caller.User != state.Scenario.Owner && !Caller.IsAdmin(caller, config))
                    return ErrorResults.Forbidden("NOT_OWNER", $"Only {state.Scenario.Owner}, the scenario's owner, or an admin publishes it.");

                var doc = ScenarioDocument.Build(state.ScenarioType, state.Scenario, state.Decisions, await db.Enclaves.FirstOrDefaultAsync(e => e.ScenarioId == id, ct));
                try
                {
                    var result = await publisher.Publish(doc, state.Scenario.Slug, state.Scenario.Owner, state.Scenario.Created, state.Scenario.Updated, ct);
                    return Results.Ok(new ChroniclePublished(result.ChronicleId, result.Entries, new ChroniclePartCounts(result.World, result.Intent), result.Subscribers, result.Url));
                }
                catch (ChronicleException e)
                {
                    return Results.Json(new ApiError("CHRONICLE_FAILED", e.Message), statusCode: e.Status);
                }
            })
            .WithTags(tag);
}
