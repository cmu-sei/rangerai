using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using RangerAi.Handlers;
using RangerAi.Infrastructure;
using RangerAi.Infrastructure.Models;
using RangerAi.Services;

namespace RangerAi.Controllers;

[ApiController]
public class ChatController : ControllerBase
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    [HttpPost("/api/chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        _log.Info(
            $"Incoming ChatRequest: model={request?.Model ?? "<null>"}, messages={request?.Messages?.Count ?? 0}, stream={request?.Stream}");

        if (request == null)
        {
            _log.Trace("Request is null");
            return BadRequest(new { error = "Invalid payload" });
        }

        // 1. Check local handlers first
        var handlerInstances = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => (IHandler)Activator.CreateInstance(t))
            .OrderBy(h => h != null && h.GetType().Name == "Ai" ? 1 : 0) // "Ai" handler is always last
            .ToList();

        foreach (var handler in handlerInstances)
        {
            if (handler != null)
            {
                handler.ChatRequest = request;
                if (handler.ShouldAnswer)
                {
                    _log.Info("Handling locally with " + handler.GetType().Name);
                    var result = await handler.Handle();
                    var json = JsonSerializer.Serialize(result,
                        new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                    _log.Trace("Local handler produced result: " + json);
                    return Content(json, "application/json");
                }
            }
            else
            {
                _log.Trace("Handler is null");
            }
        }

        // 2. Not handled locally, is workflow?
        var isWorkflow = !string.IsNullOrEmpty(request.Model)
                         && request.Model.StartsWith("workflow:", StringComparison.OrdinalIgnoreCase);

        if (isWorkflow)
        {
            _log.Info("Workflow model detected: " + request.Model);

            var workflow = await new ConfigurationService().GetWorkflow(request.Model);
            if (workflow == null)
            {
                _log.Warn("Workflow not found for model: " + request.Model);
                return NotFound(new { error = $"Workflow {request.Model} not found" });
            }

            var target = $"{ApplicationDetails.GetWorkflowHost()}/webhook/{workflow.Path}";
            _log.Info("Redirecting to n8n workflow endpoint: " + target);
            return RedirectPreserveMethod(target);
        }

        _log.Info("Request is not a workflow, will fallback to Ollama");

        // 3. Fallback to Ollama
        var ollamaTarget = ApplicationDetails.GetOllamaHost() + Request.Path + Request.QueryString;
        _log.Info("Redirecting to Ollama endpoint: " + ollamaTarget);
        return RedirectPreserveMethod(ollamaTarget);
    }
}
