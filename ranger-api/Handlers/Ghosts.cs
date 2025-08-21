using System.Text.Json;
using System.Text.RegularExpressions;
using RangerAi.Infrastructure;
using RangerAi.Infrastructure.Extensions;
using RangerAi.Infrastructure.Models;
using RangerAi.Infrastructure.Options;

namespace RangerAi.Handlers;

public class Ghosts : HandlerBase
{
    protected override Regex MatchPattern { get; } = new(
        @"^/ghosts ",
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

        var command = tokens[1];
        var args = tokens.Skip(2).ToList();

        var baseUrl = ApplicationDetails.GetGhostsHost();
        var client = new HttpClient();

        // Command: ghosts machines
        if ((command == "machines" && args.Count == 0) ||
            (command == "machines" && args.Count == 1 && args[0] == "list"))
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/machines");
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/markdown"));

                var o = await client.SendAsync(request);
                var content = await o.Content.ReadAsStringAsync();

                response.Message.Content = content;
            }
            catch (Exception ex)
            {
                response.Message.Content = $"Error: {ex.Message}";
            }
        }

        if (command == "machines" && args.Count > 1 && args[0] == "info")
        {
            // ghosts machines info efbb0bc8-56fd-4b64-8539-296c0ee135c0
            //response.Message.Content = $"GET /api/machines/{args[1]} — Gets a specific machine by its Guid";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/machines/{args[1]}");
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/markdown"));

                var o = await client.SendAsync(request);
                var content = await o.Content.ReadAsStringAsync();

                response.Message.Content = content;
            }
            catch (Exception ex)
            {
                response.Message.Content = $"Error: {ex.Message}";
            }
        }

        // Command: ghosts machine create
        if (command == "machine" && args.Count == 1 && args[0] == "create")
            response.Message.Content = "POST /api/machines — Create a machine (client must later check in)";

        // Command: ghosts machine info <id>

        // Command: ghosts machine update <id>
        if (command == "machine" && args.Count >= 2 && args[0] == "update")
            response.Message.Content = $"PUT /api/machines/{args[1]} — Updates a machine's information";

        // Command: ghosts machine delete <id>
        if (command == "machine" && args.Count >= 2 && args[0] == "delete")
            response.Message.Content = $"DELETE /api/machines/{args[1]} — Deletes a machine";

        // Command: ghosts machine activity <id>
        if (command == "machine" && args.Count >= 2 && args[0] == "activity")
            response.Message.Content = $"GET /api/machines/{args[1]}/activity — Lists activity for this machine";

        // Command: ghosts machine health <id>
        if (command == "machine" && args.Count >= 2 && args[0] == "health")
            response.Message.Content = $"GET /api/machines/{args[1]}/health — Gets health for this machine";


        if ((command == "npcs" && args.Count == 0) ||
            (command == "npcs" && args.Count == 1 && args[0] == "list"))
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/npcs/list");
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/markdown"));

                var o = await client.SendAsync(request);
                var content = await o.Content.ReadAsStringAsync();

                response.Message.Content = content;
            }
            catch (Exception ex)
            {
                response.Message.Content = $"Error: {ex.Message}";
            }
        }

        if (command == "command")
        {
            try
            {
                var lastMessage = this.ChatRequest.Messages.Last();
                lastMessage.Content = lastMessage.Content.Replace("/ghosts command", "").Trim();
                this.ChatRequest.Messages.Clear();
                this.ChatRequest.Messages.Add(lastMessage);

                var chatResponse = await GetGhostsIntent(this.ChatRequest);
                var actionRequest = JsonSerializer.Deserialize<ActionRequest>(chatResponse.Message.Content, JsonTransformationOptions.JsonSerializerOptions);
                if (actionRequest != null)
                {
                    actionRequest.original = chatResponse.Message.Content;
                    await IssueGhostsNpcCommand(actionRequest);

                    chatResponse = new ChatResponse
                    {
                        Message =
                        {
                            Content = $"I asked {actionRequest.who.Capitalize()} to use {actionRequest.handler} to {actionRequest.action} because {actionRequest.reasoning} with a scale of {actionRequest.scale}."
                        }
                    };
                }

                return chatResponse;
            }
            catch (Exception e)
            {
                response.Message.Content = $"Is the intent workflow available? Error: {e.Message}";
                Console.WriteLine(e);
            }
        }

        if (string.IsNullOrEmpty(response.Message.Content))
            response.Message.Content = $"unrecognized command: {string.Join(" ", tokens)}";

        return response;
    }

    private async Task<ChatResponse> GetGhostsIntent(ChatRequest r)
    {
        r.Stream = false;
        r.Model = "ghosts-intent";

        //var requestPayload = JsonSerializer.Serialize(r, JsonTransformationOptions.JsonSerializerOptions);
        var jsonString = JsonSerializer.Serialize(r);

        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, ApplicationDetails.GetWorkflowHost() + "/webhook/ghosts-intent")
        {
            Content = new StringContent(jsonString, null, "application/json")
        };
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ChatResponse>(raw, JsonTransformationOptions.JsonSerializerOptions);

        // model sometimes acts up and spits out non-json values
        if (result != null)
        {
            result.Message.Content = result.Message.Content.Replace("```json", "").Replace("```", "");
            Console.WriteLine(result);

            return result;
        }

        return new ChatResponse() { Message = new ChatMessage() { Content = "Intent could not be found — invalid response" } };
    }

    private async Task IssueGhostsNpcCommand(ActionRequest r)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, ApplicationDetails.GetGhostsHost() + "/api/npcs/command")
        {
            Content = JsonContent.Create(r)
        };
        _ = await client.SendAsync(request);
    }
}
