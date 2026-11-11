// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Endpoints.GraySpace;

/// <summary>What GraySpace offers an enclave, each part as GraySpace wrote it; <c>problems</c> says why a part is missing.</summary>
public record OffersView(JsonNode? Server, JsonNode? Capabilities, JsonNode? Base, JsonNode? Versions, JsonNode? AttachmentPoints, JsonNode? Map, JsonNode? Slices, Dictionary<string, string> Problems);

/// <summary>
/// GET /grayspace/offers — the connected deployment's server part (images, roles, jobs, capacity), the deployed base's
/// capabilities (DNS, CAs, ISPs), base, the base versions it ships, attachment points, map and enclaves. The deployment
/// must answer; any part GraySpace can't give, such as capabilities with no base deployed, is left out, with its reason.
/// The server part is GET /capabilities without what a base adds. With no base deployed it is asked for a shipped
/// version, and its <c>base</c> is null.
/// </summary>
public class OffersEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapGet("/grayspace/offers", async (GraySpaceClient grayspace, CancellationToken ct) =>
            {
                await grayspace.Check(ct);
                var problems = new Dictionary<string, string>();

                async Task<JsonNode?> Part(string name, string path)
                {
                    try
                    {
                        return await grayspace.Get(path, ct);
                    }
                    catch (GraySpaceException e)
                    {
                        problems[name] = e.Message;
                        return null;
                    }
                }

                var capabilities = await Part("capabilities", "capabilities");
                var versions = await Part("versions", "base/versions");
                var server = capabilities;
                if (server is null)
                {
                    var shipped = (versions as JsonArray)?.Select(v => (string?)v?["name"]).FirstOrDefault(name => name is not null);
                    server = shipped is null ? null : await Part("server", $"capabilities?version={Uri.EscapeDataString(shipped)}");
                    if (server is JsonObject undeployed)
                        undeployed["base"] = null;
                    else
                        problems.TryAdd("server", problems.GetValueOrDefault("capabilities") ?? "GraySpace gave no capabilities.");
                }

                return Results.Ok(new OffersView(server, capabilities, await Part("base", "base"), versions,
                    await Part("attachmentPoints", "attachment-points"), await Part("map", "map"), await Part("slices", "slices"), problems));
            })
            .AddEndpointFilter<GraySpaceFilter>().WithTags(tag);
}
