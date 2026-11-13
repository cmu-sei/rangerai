// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ScenarioBuilder.Api.Domain;
using ScenarioBuilder.Api.Endpoints.GraySpace;
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Endpoints.Enclaves;

/// <summary>
/// POST /scenarios/{id}/enclave/draft — drafts the enclave from the scenario's stage 3 answers and what the connected
/// GraySpace offers (<see cref="EnclaveDrafter"/>), and saves it as the draft. Its slug and prefix stay; everything
/// else is replaced. 422 NOTHING_TO_DRAFT when stage 3 has no segments or hosts yet.
/// </summary>
public class DraftEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapPost("/scenarios/{id:int}/enclave/draft", async (int id, HttpContext http, BuilderDb db, ScenarioTypeCatalog scenarioTypes, GraySpaceClient grayspace,
                IOptions<EnclaveDraftOptions> options, CancellationToken ct) =>
            {
                if (await ScenarioState.Load(db, scenarioTypes, id, ct) is not { } state)
                    return ScenarioState.Missing(id);
                if (await EnclaveRules.Find(db, id, ct) is not { } enclave)
                    return ScenarioState.Missing(id);
                var capabilities = await grayspace.Get("capabilities", ct) as JsonObject ?? [];
                var points = await grayspace.Get("attachment-points", ct) as JsonArray ?? [];
                var doc = ScenarioDocument.Build(state.ScenarioType, state.Scenario, state.Decisions);
                try
                {
                    enclave.Spec = EnclaveDrafter.Draft(doc, EnclaveRules.Spec(enclave), capabilities, points, options.Value).ToJsonString();
                }
                catch (EnclaveDrafter.NothingToDraftException e)
                {
                    return ErrorResults.Unprocessable("NOTHING_TO_DRAFT", e.Message);
                }
                EnclaveRules.Touch(db, enclave, http);
                await db.SaveChangesAsync(ct);
                return Results.Ok(EnclaveView.From(enclave));
            })
            .AddEndpointFilter<GraySpaceFilter>().WithTags(tag);
}
