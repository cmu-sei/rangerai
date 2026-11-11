// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ScenarioBuilder.Api.Domain;
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Endpoints.GraySpace;

public record ConnectGraySpace(string? Url, string? Token);

/// <summary>
/// PUT /grayspace — an admin connects GraySpace, or moves it, by its address and its GRAYSPACE_API_TOKEN. The address is
/// a deployment's base URL (<c>http://grayspace.local:8080/deployments/abc</c>); given the server's alone, it becomes
/// its default deployment's. They are saved only once GraySpace answers GET /deployments/{id} with them, which it does
/// with or without a deployed base.
/// </summary>
public class ConnectEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapPut("/grayspace", async (ConnectGraySpace body, HttpContext http, BuilderDb db, GraySpaceClient grayspace, CancellationToken ct) =>
            {
                if (!Uri.TryCreate(body.Url?.Trim(), UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https"))
                    return ErrorResults.BadRequest("URL_INVALID", "Give GraySpace's address in full, starting with http:// or https://, such as http://grayspace.local:8080.");
                if (string.IsNullOrWhiteSpace(body.Token))
                    return ErrorResults.BadRequest("TOKEN_REQUIRED", "Give GraySpace's token: its GRAYSPACE_API_TOKEN.");

                var url = Address(body.Url!);
                var token = body.Token.Trim();
                if (GraySpaceClient.Split(url).Deployment is null)
                {
                    var deployments = JsonNode.Parse(await grayspace.Send(url, token, HttpMethod.Get, "deployments", null, ct)) as JsonArray ?? [];
                    var chosen = deployments.FirstOrDefault(d => d?["default"]?.GetValueKind() == JsonValueKind.True) ?? (deployments.Count == 1 ? deployments[0] : null);
                    if ((string?)chosen?["id"] is not { Length: > 0 } id)
                        return ErrorResults.BadRequest("DEPLOYMENT_REQUIRED", deployments.Count == 0
                            ? $"GraySpace at {url} has no deployment yet. Create one in GraySpace, then connect."
                            : $"GraySpace at {url} has {deployments.Count} deployments and no default. Give one's address, such as {url}/deployments/{(string?)deployments[0]?["id"]}.");
                    url = $"{url}/deployments/{Uri.EscapeDataString(id)}";
                }
                await grayspace.Check(url, token, ct);

                var connection = await db.GraySpaceConnections.FirstOrDefaultAsync(ct);
                if (connection is null)
                    db.GraySpaceConnections.Add(connection = new GraySpaceConnection());
                connection.Url = url;
                connection.Token = token;
                connection.ConnectedBy = Caller.From(http).User;
                connection.Connected = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return Results.Ok(GraySpaceView.From(connection, answering: true));
            })
            .AddEndpointFilter<AdminOnly>().AddEndpointFilter<GraySpaceFilter>().WithTags(tag);

    /// <summary>The address without a trailing slash or /api/v1, which every call adds.</summary>
    private static string Address(string url)
    {
        var address = url.Trim().TrimEnd('/');
        return address.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase) ? address[..^"/api/v1".Length] : address;
    }
}
