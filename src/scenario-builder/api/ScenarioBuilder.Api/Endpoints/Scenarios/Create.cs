// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ScenarioBuilder.Api.Domain;
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Endpoints.Scenarios;

public record CreateScenario(string? Name, string? ScenarioTypeId);

public record ScenarioSummary(
    int Id, string Slug, string Name, string ScenarioTypeId, string ScenarioTypeVersion, string Owner, int Stage, DateTime Created, DateTime Updated, bool Archived)
{
    public static ScenarioSummary From(Scenario s) =>
        new(s.Id, s.Slug, s.Name, s.ScenarioTypeId, s.ScenarioTypeVersion, s.Owner, s.Stage, s.Created, s.Updated, s.Archived);
}

/// <summary>
/// POST /scenarios — a new scenario on a scenario type's first stage. The caller becomes its owner. Its Chronicle (id:
/// the slug) is created with it, so the Chronicle lists the scenario from the start; a Chronicle that doesn't answer
/// doesn't stop the scenario, and the Finish page's publish creates the record then.
/// </summary>
public class CreateEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapPost("/scenarios", async (CreateScenario body, HttpContext http, ScenarioTypeCatalog scenarioTypes, BuilderDb db,
            ChronicleClient chronicle, ChroniclePublisher publisher, ILogger<CreateEndpoint> log, CancellationToken ct) =>
        {
            var name = body.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                return ErrorResults.BadRequest("NAME_REQUIRED", "Give the scenario a name.");
            var scenarioType = scenarioTypes.Find(body.ScenarioTypeId ?? string.Empty);
            if (scenarioType is null)
                return ErrorResults.BadRequest("SCENARIO_TYPE_UNKNOWN",
                    $"There is no scenario type '{body.ScenarioTypeId}'. Scenario types: {string.Join(", ", scenarioTypes.Offered.Select(k => k.Id))}.");
            if (scenarioTypes.IsRetired(scenarioType.Id))
                return ErrorResults.BadRequest("SCENARIO_TYPE_RETIRED",
                    $"'{scenarioType.Name}' is retired, so new scenarios can't use it. Scenario types: {string.Join(", ", scenarioTypes.Offered.Select(k => k.Id))}.");

            var taken = (await db.Scenarios.Select(s => s.Slug).ToListAsync(ct)).ToHashSet();
            var caller = Caller.From(http);
            var now = DateTime.UtcNow;
            var scenario = new Scenario
            {
                Slug = Slug.Unique(name, taken),
                Name = name,
                ScenarioTypeId = scenarioType.Id,
                ScenarioTypeVersion = scenarioType.Version,
                Owner = caller.User,
                Created = now,
                Updated = now
            };
            db.Scenarios.Add(scenario);
            await db.SaveChangesAsync(ct);

            // A question that defaults to the scenario's name starts answered with it; the owner can change it later.
            var value = JsonValue.Create(name).ToJsonString(Ledger.Storage);
            foreach (var stage in scenarioType.Stages)
                foreach (var q in stage.Questions.Where(q => q.Default == ScenarioType.ScenarioNameDefault))
                {
                    db.Edits.Add(new Edit { ScenarioId = scenario.Id, Target = q.Id, After = value, By = caller.User, Via = caller.Via, Time = now });
                    db.Decisions.Add(new Decision
                    {
                        ScenarioId = scenario.Id, QuestionId = q.Id, Stage = stage.Number, Value = value,
                        Who = caller.IsAgent ? Who.Proposed : Who.Stated, DecidedBy = caller.User, Via = caller.Via,
                        Reason = "Defaults to the scenario's name.", Time = now
                    });
                }
            await db.SaveChangesAsync(ct);

            if (chronicle.PublishOnCreate)
                await PublishToChronicle(scenario, scenarioType, db, publisher, log, ct);
            return Results.Created($"/api/scenarios/{scenario.Id}", ScenarioSummary.From(scenario));
        }).WithTags(tag);

    private static async Task PublishToChronicle(Scenario scenario, ScenarioType scenarioType, BuilderDb db, ChroniclePublisher publisher,
        ILogger log, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var decisions = await db.Decisions.Where(d => d.ScenarioId == scenario.Id).ToListAsync(ct);
            var doc = ScenarioDocument.Build(scenarioType, scenario, decisions);
            await publisher.Publish(doc, scenario.Slug, scenario.Owner, scenario.Created, scenario.Updated, timeout.Token);
        }
        catch (Exception e) when (e is ChronicleException || (e is OperationCanceledException && !ct.IsCancellationRequested))
        {
            log.LogWarning("Scenario {Slug} was created, but its Chronicle wasn't: {Reason}", scenario.Slug, e.Message);
        }
    }
}
