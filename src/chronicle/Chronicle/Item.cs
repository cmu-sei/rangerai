// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Reflection;
using System.Text.Json.Serialization;

namespace Chronicle;

/// <summary>
/// One thing a Chronicle holds: a device, an objective, an inject delivered, a lesson. Every item has an id that is
/// unique in its Chronicle, says who may see it, and says whether it was stated or proposed. Its <c>kind</c> says
/// what it is; each kind belongs to one or more parts.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
// World
[JsonDerivedType(typeof(Side), "side")]
[JsonDerivedType(typeof(Organization), "organization")]
[JsonDerivedType(typeof(Network), "network")]
[JsonDerivedType(typeof(NetworkLink), "networkLink")]
[JsonDerivedType(typeof(Device), "device")]
[JsonDerivedType(typeof(OperatingSystem), "operatingSystem")]
[JsonDerivedType(typeof(Service), "service")]
[JsonDerivedType(typeof(Application), "application")]
[JsonDerivedType(typeof(CyberSystem), "system")]
[JsonDerivedType(typeof(Data), "data")]
[JsonDerivedType(typeof(Persona), "persona")]
[JsonDerivedType(typeof(PersonaPool), "personaPool")]
[JsonDerivedType(typeof(Vulnerability), "vulnerability")]
[JsonDerivedType(typeof(Defense), "defense")]
[JsonDerivedType(typeof(Fact), "fact")]
// Intent
[JsonDerivedType(typeof(Objective), "objective")]
[JsonDerivedType(typeof(TeachingPoint), "teachingPoint")]
[JsonDerivedType(typeof(Rule), "rule")]
[JsonDerivedType(typeof(Term), "term")]
[JsonDerivedType(typeof(PlannedEvent), "plannedEvent")]
// As Built
[JsonDerivedType(typeof(Deployment), "deployment")]
[JsonDerivedType(typeof(Realization), "realization")]
[JsonDerivedType(typeof(Content), "content")]
// As Run
[JsonDerivedType(typeof(Milestone), "milestone")]
[JsonDerivedType(typeof(CyberActivity), "cyberActivity")]
[JsonDerivedType(typeof(InfluenceActivity), "influenceActivity")]
[JsonDerivedType(typeof(InjectDelivery), "injectDelivery")]
[JsonDerivedType(typeof(ParticipantAction), "participantAction")]
[JsonDerivedType(typeof(Observation), "observation")]
// Lessons
[JsonDerivedType(typeof(Assessment), "assessment")]
[JsonDerivedType(typeof(Lesson), "lesson")]
// Any part
[JsonDerivedType(typeof(Extension), "extension")]
public abstract record Item
{
    /// <summary>Unique in the Chronicle, across every part. Letters, digits, '.', '_', ':' and '-'; it never changes.</summary>
    [JsonPropertyOrder(-2)]
    public required string Id { get; init; }

    /// <summary>
    /// The lowest audience that may see the item. Absent means excon: an item nobody marked stays with exercise
    /// control, so ground truth fails closed.
    /// </summary>
    [JsonPropertyOrder(-1)]
    public Audience Visibility { get; init; } = Audience.Excon;

    /// <summary>
    /// Personas, persona pools, sides or organizations that also know this item, whatever its visibility: what a
    /// simulated bank clerk knows about their own bank, for example.
    /// </summary>
    public IReadOnlyList<string> KnownBy { get; init; } = [];

    /// <summary>Stated by a person, or proposed and not yet confirmed. Absent means stated.</summary>
    public Standing Standing { get; init; } = Standing.Stated;

    /// <summary>Every other item this one names, for checking that references resolve.</summary>
    public virtual IEnumerable<string> References() => KnownBy;

    /// <summary>The kind, as JSON writes it.</summary>
    [JsonIgnore]
    public string Kind => KindOf(GetType());

    /// <summary>The parts an item of this kind may be in.</summary>
    [JsonIgnore]
    public IReadOnlyList<Part> Parts => PartsOf(GetType());

    private static readonly Dictionary<Type, string> Kinds = typeof(Item)
        .GetCustomAttributes<JsonDerivedTypeAttribute>()
        .ToDictionary(a => a.DerivedType, a => (string)a.TypeDiscriminator!);

    /// <summary>Every kind, and the type that carries it.</summary>
    public static IReadOnlyDictionary<string, Type> AllKinds { get; } = Kinds.ToDictionary(k => k.Value, k => k.Key);

    /// <summary>The kind an item type writes as.</summary>
    public static string KindOf(Type type) => Kinds[type];

    /// <summary>The parts an item type may be in; an extension may be in any.</summary>
    public static IReadOnlyList<Part> PartsOf(Type type) =>
        type.GetCustomAttribute<PartsAttribute>()?.Parts ?? Enum.GetValues<Part>();
}

/// <summary>The parts an item kind may be in.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PartsAttribute(params Part[] parts) : Attribute
{
    /// <summary>The parts.</summary>
    public IReadOnlyList<Part> Parts { get; } = parts;
}

/// <summary>
/// An item this version of the schema has no kind for: an app's own data, or a kind proposed for a later version.
/// Readers that don't know its type pass it on unchanged.
/// </summary>
public sealed record Extension : Item
{
    /// <summary>The extension's type, named by the app that owns it, such as ghosts.npcProfile.</summary>
    public required string Type { get; init; }

    /// <summary>A version of the type, if its owner versions it.</summary>
    public string? Version { get; init; }

    /// <summary>The extension's data, in whatever form its owner defines.</summary>
    public required System.Text.Json.JsonElement Data { get; init; }
}
