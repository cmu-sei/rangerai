using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RangerAi.Infrastructure;
using RangerAi.Infrastructure.Models;

namespace RangerAi.Services;

public class AiService
{
    public async Task<ChatResponse> TriggerWorkflowAsync(ChatRequest request)
    {
        request.ThreadId = Guid.NewGuid().ToString();
        request.OperationId = request.ThreadId;
        request.Stream = false;

        var client = new HttpClient();
        var ollamaUrl = $"{ApplicationDetails.GetOllamaHost()}/api/chat";
        var json = JsonSerializer.Serialize(request);

        var proxyRequest = new HttpRequestMessage(HttpMethod.Post, ollamaUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var proxyResponse = await client.SendAsync(proxyRequest);

        var responseBody = await proxyResponse.Content.ReadAsStringAsync();

        var parsed = JsonSerializer.Deserialize<OllamaResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return new ChatResponse {
            Message = new ChatMessage {
                Content = parsed?.Message?.Content ?? "[no content]"
            }
        };
    }

    public static string ExtractContent(string rawJson)
    {
        using var outerDoc = JsonDocument.Parse(rawJson);
        var root = outerDoc.RootElement;

        if (!root.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var contentElement))
            return null;

        var content = contentElement.GetString();

        if (content == null)
            return null;

        // Try to match markdown-style code block first
        var match = Regex.Match(content, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        var innerJson = match.Success ? match.Groups[1].Value : content;

        // Replace single quotes and ensure inner quotes are valid
        innerJson = innerJson.Trim().Replace("'", "\"");

        try
        {
            using var innerDoc = JsonDocument.Parse(innerJson);
            if (innerDoc.RootElement.TryGetProperty("activities", out var activities) &&
                activities.ValueKind == JsonValueKind.Array &&
                activities.GetArrayLength() > 0)
            {
                var activity = activities[0];
                if (activity.TryGetProperty("content", out var result))
                    return result.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            // Not a valid JSON payload inside "content"
            return innerJson;
        }
    }

    public class OllamaResponse
    {
        public string Model { get; set; }
        public DateTime Created_At { get; set; }
        public MessageBlock Message { get; set; }
        public bool Done { get; set; }
        // etc.
    }

    public class MessageBlock
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
