using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RangerAi.Infrastructure.Models
{
    public class ChatRequest
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("threadId")]
        public string ThreadId { get; set; }

        [JsonPropertyName("operationId")]
        public string OperationId { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        [JsonPropertyName("tools")]
        public object Tools { get; set; }

        [JsonPropertyName("format")]
        public object Format { get; set; }

        [JsonPropertyName("options")]
        public object Options { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;

        [JsonPropertyName("keep_alive")]
        public string KeepAlive { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("threadId")]
        public string ThreadId { get; set; }

        [JsonPropertyName("operationId")]
        public string OperationId { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("images")]
        public List<string> Images { get; set; } = new List<string>();

        [JsonPropertyName("tool_calls")]
        public object ToolCalls { get; set; }
    }
}
