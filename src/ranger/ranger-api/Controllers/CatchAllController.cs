using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using RangerAi.Infrastructure;

namespace RangerAi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class CatchAllController : ControllerBase
{
    [Route("{*url}", Order = 999)]
    public async Task<IActionResult> CatchAll()
    {
        var path = Request.Path.Value?.ToLowerInvariant();

        if (path != null && (
                path.StartsWith("/swagger") ||
                path.StartsWith("/api-docs") ||
                path.StartsWith("/favicon.ico") ||
                path.StartsWith("/robots.txt")))
        {
            return NotFound();
        }

        var client = new HttpClient();
        var ollamaUrl = ApplicationDetails.GetOllamaHost() + Request.Path + Request.QueryString;

        var method = new HttpMethod(Request.Method);
        var proxyRequest = new HttpRequestMessage(method, ollamaUrl);

        if (Request.ContentLength > 0)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (Request.ContentType != null)
                proxyRequest.Content = new StringContent(body, System.Text.Encoding.UTF8, Request.ContentType);
        }

        var proxyResponse = await client.SendAsync(proxyRequest);
        var responseBody = await proxyResponse.Content.ReadAsStringAsync();

        var finalBody = responseBody;
        if (path == "/api/tags")
        {
            var root = JsonNode.Parse(responseBody)?.AsObject();
            if (root?["models"] is JsonArray modelsArray)
            {
                proxyRequest = new HttpRequestMessage(HttpMethod.Get,
                    $"{ApplicationDetails.GetWorkflowHost()}/api/v1/workflows?active=true");
                proxyRequest.Headers.Add("X-N8N-API-KEY", ApplicationDetails.GetWorkflowApiKey());
                proxyResponse = await client.SendAsync(proxyRequest);
                var workFlowsBody = await proxyResponse.Content.ReadAsStringAsync();
                var workFlowObject = JsonNode.Parse(workFlowsBody);

                if (workFlowObject != null)
                {
                    foreach (var o in workFlowObject["data"].AsArray())
                    {
                        modelsArray.Add(new JsonObject
                        {
                            ["name"] = $"workflow:{o["name"]}",
                            ["model"] = $"workflow:{o["name"]}",
                            ["modified_at"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz",
                                CultureInfo.InvariantCulture),
                            ["size"] = 0,
                            ["digest"] = o["id"]?.ToString(),
                            ["details"] = new JsonObject
                            {
                                ["parent_model"] = "",
                                ["format"] = "workflow",
                                ["family"] = "workflow",
                                ["families"] = "workflow",
                                ["parameter_size"] = "0B",
                                ["quantization_level"] = "0_0"
                            },
                        });
                    }
                }
            }

            finalBody = root?.ToJsonString(new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });
        }

        return Content(finalBody ?? string.Empty,
            proxyResponse.Content.Headers.ContentType?.MediaType ?? "application/json");
    }
}
