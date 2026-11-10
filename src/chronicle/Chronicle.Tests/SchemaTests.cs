// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Chronicle.Tests;

public class SchemaTests
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Built once: JsonSchema.Net registers a schema's $id globally and refuses a second registration.</summary>
    private static readonly JsonSchema RecordSchema = JsonSchema.FromText(ChronicleSchema.Record().ToJsonString());
    private static readonly JsonSchema EntrySchema = JsonSchema.FromText(ChronicleSchema.Entry().ToJsonString());

    [Fact]
    public void Record_schema_file_is_current() =>
        Files.AssertCurrent(Files.RecordSchema, ChronicleSchema.Record().ToJsonString(Indented));

    [Fact]
    public void Entry_schema_file_is_current() =>
        Files.AssertCurrent(Files.EntrySchema, ChronicleSchema.Entry().ToJsonString(Indented));

    [Fact]
    public void Sample_file_is_current() =>
        Files.AssertCurrent(Files.Sample, ChronicleJson.Serialize(HarborSample.Build()));

    [Fact]
    public void Sample_passes_the_record_schema() =>
        AssertValid(RecordSchema, ChronicleJson.Serialize(HarborSample.Build()));

    [Fact]
    public void Every_sample_entry_passes_the_entry_schema()
    {
        var schema = EntrySchema;
        foreach (var entry in HarborSample.Build().History)
            AssertValid(schema, ChronicleJson.Serialize(entry, indented: false));
    }

    [Fact]
    public void Schema_refuses_an_unknown_kind_and_an_unknown_property()
    {
        var schema = RecordSchema;
        var record = JsonNode.Parse(ChronicleJson.Serialize(HarborSample.Build()))!;

        var badKind = record.DeepClone();
        badKind["world"]![0]!["kind"] = "spaceship";
        Assert.False(Evaluate(schema, badKind).IsValid);

        var extra = record.DeepClone();
        extra["world"]![0]!["colour"] = "blue";
        Assert.False(Evaluate(schema, extra).IsValid);
    }

    [Fact]
    public void Schema_describes_fields_and_cites_siso_tables()
    {
        var schema = ChronicleSchema.Record();
        var device = schema["$defs"]!["Device"]!;
        Assert.StartsWith("A host, server, router", (string)device["description"]!);
        var deviceTypes = device["properties"]!["deviceTypes"]!;
        Assert.Equal(683, (int)deviceTypes["x-siso-ref-010"]!["uid"]!);
        Assert.NotNull(deviceTypes["description"]);
    }

    private static void AssertValid(JsonSchema schema, string json)
    {
        var result = Evaluate(schema, JsonNode.Parse(json));
        Assert.True(result.IsValid, string.Join("\n", Errors(result)));
    }

    private static EvaluationResults Evaluate(JsonSchema schema, JsonNode? instance) =>
        schema.Evaluate(JsonDocument.Parse(instance!.ToJsonString()).RootElement,
            new EvaluationOptions { OutputFormat = OutputFormat.List });

    private static IEnumerable<string> Errors(EvaluationResults result) =>
        (result.Details ?? []).Where(d => d.Errors is { Count: > 0 })
            .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"));
}
