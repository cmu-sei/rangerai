using System.Text.Json;
using System.Text.Json.Serialization;

namespace RangerAi.Infrastructure.Options;

public static class JsonTransformationOptions
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
