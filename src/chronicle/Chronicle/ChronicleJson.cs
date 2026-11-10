// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Collections;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Linq;

namespace Chronicle;

/// <summary>
/// How a Chronicle is written as JSON: camelCase names, Ranger's enumerations as camelCase strings, SISO's values as
/// SISO writes them, nulls and empty lists left out, unknown properties refused.
/// </summary>
public static class ChronicleJson
{
    /// <summary>The options every reader and writer uses.</summary>
    public static JsonSerializerOptions Options { get; } = Create(indented: true);

    /// <summary>The same, on one line: for entries on the wire.</summary>
    public static JsonSerializerOptions Compact { get; } = Create(indented: false);

    /// <summary>Writes a record or an entry.</summary>
    public static string Serialize<T>(T value, bool indented = true) =>
        JsonSerializer.Serialize(value, indented ? Options : Compact);

    /// <summary>Reads a record or an entry.</summary>
    /// <exception cref="JsonException">The JSON isn't a valid <typeparamref name="T"/>.</exception>
    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException($"The JSON is null, not a {typeof(T).Name}.");

    /// <summary>
    /// Puts the spec's settings on options someone else owns, such as ASP.NET Core's, so an HTTP body reads and writes
    /// exactly as <see cref="Options"/> does.
    /// </summary>
    public static void Apply(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = false;
        options.NumberHandling = JsonNumberHandling.Strict;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.RespectNullableAnnotations = true;
        options.RespectRequiredConstructorParameters = true;
        options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.AllowOutOfOrderMetadataProperties = true;
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { SkipEmptyCollections } };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    }

    private static JsonSerializerOptions Create(bool indented)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented };
        Apply(options);
        options.MakeReadOnly();
        return options;
    }

    private static void SkipEmptyCollections(JsonTypeInfo type)
    {
        foreach (var property in type.Properties)
            if (property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                property.ShouldSerialize = (_, value) => value is not ICollection { Count: 0 };
    }
}

/// <summary>
/// The JSON Schema, generated from this library's types so the two can't drift. Descriptions come from the XML
/// documentation; every named type is a $def.
/// </summary>
public static class ChronicleSchema
{
    /// <summary>The schema for a whole record.</summary>
    public static JsonObject Record() =>
        For(typeof(ChronicleRecord), $"urn:ranger:chronicle:record:v{Major}", $"Chronicle record, schema {ChronicleRecord.CurrentSchemaVersion}");

    /// <summary>The schema for one entry, as apps publish it.</summary>
    public static JsonObject Entry() =>
        For(typeof(Entry), $"urn:ranger:chronicle:entry:v{Major}", $"Chronicle entry, schema {ChronicleRecord.CurrentSchemaVersion}");

    private static string Major => ChronicleRecord.CurrentSchemaVersion.Split('.')[0];

    private static JsonObject For(Type root, string id, string title)
    {
        var docs = Docs.Load();
        var defs = new SortedDictionary<string, JsonNode>(StringComparer.Ordinal);
        var exporter = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, node) => Transform(context, node, docs, defs)
        };
        var schema = (JsonObject)ChronicleJson.Options.GetJsonSchemaAsNode(root, exporter);

        var result = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"] = id,
            ["title"] = title
        };
        foreach (var (key, value) in schema.ToList())
        {
            schema.Remove(key);
            result[key] = value;
        }
        result["properties"]!["schemaVersion"]?.AsObject().Add("pattern", $"^{Major}\\.[0-9]+\\.[0-9]+$");
        result["$defs"] = new JsonObject(defs.Select(d => KeyValuePair.Create(d.Key, (JsonNode?)d.Value)));
        return result;
    }

    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode node, Docs docs, IDictionary<string, JsonNode> defs)
    {
        var type = context.TypeInfo.Type;
        var member = context.PropertyInfo?.AttributeProvider as MemberInfo;

        if (node is JsonObject obj)
        {
            // Items of a SISO enumeration list carry the table too, so annotate the property's node.
            if (member?.GetCustomAttribute<SisoEnumerationAttribute>() is { } siso)
                obj["x-siso-ref-010"] = new JsonObject { ["uid"] = siso.Uid, ["table"] = siso.Table };
        }

        // Hoist every type of ours into $defs, and point at it.
        var hoist = type.Assembly == typeof(ChronicleSchema).Assembly && context.Path.Length > 0 && node is JsonObject;
        if (hoist)
        {
            var name = type.Name;
            var def = (JsonObject)node.DeepClone();
            def.Remove("description");
            def.Remove("x-siso-ref-010");
            var nullable = StripNull(def);
            if (docs.For(type) is { } typeDoc)
                def.Insert(0, "description", typeDoc);
            defs.TryAdd(name, def);

            JsonObject reference = new() { ["$ref"] = $"#/$defs/{name}" };
            node = nullable ? new JsonObject { ["anyOf"] = new JsonArray(reference, new JsonObject { ["type"] = "null" }) } : reference;
        }

        if (member is not null && docs.For(member) is { } memberDoc && node is JsonObject target)
        {
            target.Remove("description");
            target.Insert(0, "description", memberDoc);
        }
        return node;
    }

    /// <summary>Removes "null" from a node's type; true if it was there.</summary>
    private static bool StripNull(JsonObject node)
    {
        if (node["type"] is not JsonArray types || !types.Any(t => t?.GetValue<string>() == "null"))
            return false;
        var rest = types.Select(t => t!.GetValue<string>()).Where(t => t != "null").ToList();
        node["type"] = rest.Count == 1 ? rest[0] : new JsonArray(rest.Select(t => (JsonNode)t).ToArray());
        if (node["enum"] is JsonArray values)
            foreach (var n in values.Where(v => v is null).ToList())
                values.Remove(n);
        return true;
    }

    /// <summary>The library's XML documentation, by member.</summary>
    private sealed class Docs(Dictionary<string, string> summaries)
    {
        public static Docs Load()
        {
            var path = Path.ChangeExtension(typeof(ChronicleSchema).Assembly.Location, ".xml");
            if (!File.Exists(path))
                return new Docs([]);
            var summaries = XDocument.Load(path).Descendants("member")
                .Where(m => m.Element("summary") is not null)
                .ToDictionary(m => (string)m.Attribute("name")!, m => Text(m.Element("summary")!));
            return new Docs(summaries);
        }

        public string? For(Type type) => summaries.GetValueOrDefault($"T:{Name(type)}");

        public string? For(MemberInfo member) => member.DeclaringType is null
            ? null
            : summaries.GetValueOrDefault($"P:{Name(member.DeclaringType)}.{member.Name}");

        private static string Name(Type type) => type.FullName!.Replace('+', '.');

        private static string Text(XElement summary)
        {
            foreach (var see in summary.Descendants("see").ToList())
                see.ReplaceWith(((string?)see.Attribute("cref"))?.Split('.').Last() ?? see.Value);
            return string.Join(' ', summary.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
