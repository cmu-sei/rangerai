using System.Text;
using System.Text.Json;
using NLog;
using RangerAi.Infrastructure.Models;
using RangerAi.Infrastructure;
using RangerAi.Services;

namespace RangerAi.Handlers
{
    /// <summary>
    /// Handler that routes workflow models to n8n and other models to Ollama with streaming support.
    /// </summary>
    public class Ai : HandlerBase
    {
        public override bool ShouldAnswer => true;
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public override async Task<ChatResponse> Handle()
        {
            // Workflow models -> n8n webhook
            if (ChatRequest.Model?.StartsWith("workflow:", StringComparison.OrdinalIgnoreCase) == true)
            {
                var configService = new ConfigurationService();
                var workflow = await configService.GetWorkflow(ChatRequest.Model);
                if (workflow != null)
                {
                    _log.Trace($"{workflow.Id} : {workflow.Name}");
                    using var client = new HttpClient();
                    var method = new HttpMethod(workflow.Method.ToUpperInvariant());
                    var url = $"{ApplicationDetails.GetWorkflowHost()}/webhook/{workflow.Path}";
                    _log.Trace($"Making workflow request to {url}");

                    var req = new HttpRequestMessage(method, url)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(ChatRequest), Encoding.UTF8, "application/json")
                    };

                    var resp = await client.SendAsync(req);
                    resp.EnsureSuccessStatusCode();
                    var respBody = await resp.Content.ReadAsStringAsync();
                    _log.Trace(respBody);

                    return new ChatResponse { Message = new ChatMessage { Content = respBody } };
                }

                _log.Info($"Workflow not found: {ChatRequest.Model}");
                return new ChatResponse { Message = new ChatMessage { Content = $"Workflow {ChatRequest.Model} not found." } };
            }

            // Other models -> Ollama streaming
            using var ollamaClient = new HttpClient();
            var ollamaUrl = $"{ApplicationDetails.GetOllamaHost()}/api/chat";
            var payload = JsonSerializer.Serialize(ChatRequest);
            _log.Trace($"Sending to Ollama at {ollamaUrl}");

            using var requestMsg = new HttpRequestMessage(HttpMethod.Post, ollamaUrl);
            requestMsg.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await ollamaClient.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var sb = new StringBuilder();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
                    line = line.Substring(6);

                if (line.Trim() == "[DONE]")
                    break;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("message", out var msgElem) &&
                        msgElem.TryGetProperty("content", out var contentElem))
                    {
                        sb.Append(contentElem.GetString());
                    }
                }
                catch (JsonException ex)
                {
                    _log.Warn($"Malformed Ollama chunk: {ex.Message}");
                }
            }

            var full = sb.ToString();
            _log.Trace($"Ollama stream complete: {full}");
            return new ChatResponse { Message = new ChatMessage { Content = full } };
        }
    }
}
