using System.Text.Json;
using System.Text.RegularExpressions;
using RangerAi.Infrastructure;
using RangerAi.Infrastructure.Models;
using RangerAi.Infrastructure.Options;

namespace RangerAi.Handlers;

public class Scenarios : HandlerBase
{
    protected override Regex MatchPattern { get; } = new(
        @"^/scenario ",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public override async Task<ChatResponse> Handle()
    {
        var response = new ChatResponse();

        var input = this.ChatRequest.Messages.Last().Content;
        if (string.IsNullOrEmpty(input))
            response.Message.Content = "no input";

        var tokens = Regex.Matches(input!, @"[\""].+?[\""]|[^\s]+")
            .Select(m => m.Value.Trim('"'))
            .ToList();

        if (tokens.Count < 2)
        {
            response.Message.Content = "no command specified";
            return response;
        }

        try
        {
            var lastMessage = this.ChatRequest.Messages.Last();
            lastMessage.Content = lastMessage.Content.Replace("/scenario ", "").Trim();
            this.ChatRequest.Messages.Clear();
            this.ChatRequest.Messages.Add(lastMessage);

            var chatResponse = await GetScenario(this.ChatRequest);
            return chatResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (string.IsNullOrEmpty(response.Message.Content))
            response.Message.Content = $"unrecognized command: {string.Join(" ", tokens)}";

        return response;
    }

    private async Task<ChatResponse> GetScenario(ChatRequest r)
    {
        r.Stream = false;
        r.Model = "scenarios";

        var requestPayload = JsonSerializer.Serialize(r, JsonTransformationOptions.JsonSerializerOptions);

        var jsonString = JsonSerializer.Serialize(r);

        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, ApplicationDetails.GetWorkflowHost() + "/webhook/scenarios")
        {
            Content = new StringContent(jsonString, null, "application/json")
        };
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();

        var result = new ChatResponse();

        using var doc = JsonDocument.Parse(raw);
        var output = doc.RootElement.GetProperty("output").GetString();
        result.Message.Content = output;

        // model sometimes acts up and spits out non-json values
        result.Message.Content = result.Message.Content.Replace("```json", "").Replace("```", "");
        Console.WriteLine(result);

        return result;
    }
}
