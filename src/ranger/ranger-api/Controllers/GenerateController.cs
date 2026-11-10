using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace RangerAi.Controllers;

//[Produces("application/json")]
[Route("api/generate")]
[ApiController]
public class GenerateController : ControllerBase
{
    [HttpPost]
    public async Task Post([FromBody] GenerateRequest request)
    {
        if (request.Stream == true)
        {
            Response.ContentType = "text/event-stream";
            var writer = new StreamWriter(Response.Body, Encoding.UTF8);

            // Simulate chunked response
            var chunks = new[] { "This ", "is ", "a ", "streamed ", "response." };
            foreach (var chunk in chunks)
            {
                var payload = JsonSerializer.Serialize(new
                {
                    response = chunk,
                    done = false
                });
                await writer.WriteAsync($"data: {payload}\n\n");
                await writer.FlushAsync();
                await Task.Delay(300); // simulate thinking
            }

            var donePayload = JsonSerializer.Serialize(new { done = true });
            await writer.WriteAsync($"data: {donePayload}\n\n");
            await writer.FlushAsync();
        }
        else
        {
            var response = new GenerateResponse
            {
                Model = request.Model,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                Response = "This is a non-streamed response.",
                Done = true,
                Context = new List<int> { 1, 2, 3 },
                TotalDuration = 1234567,
                LoadDuration = 456789,
                PromptEvalCount = 6,
                PromptEvalDuration = 234567,
                EvalCount = 20,
                EvalDuration = 345678
            };

            await Response.WriteAsJsonAsync(response);
        }
    }

    public class GenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("system")]
        public string SystemPrompt { get; set; }

        [JsonPropertyName("context")]
        public List<int> Context { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }

        [JsonPropertyName("raw")]
        public bool? Raw { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, object> Options { get; set; }
    }

    public class GenerateResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("response")]
        public string Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("context")]
        public List<int> Context { get; set; }

        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }
    }
}
