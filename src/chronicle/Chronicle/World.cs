// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;

namespace Chronicle;

// The World: what the world looks like. Cyber objects follow the Cyber DEM's Objects model (SISO-STD-025.1-2023);
// sides follow MSDL's and C2SIM's force sides; the rest is what the Cyber DEM doesn't model.

/// <summary>
/// A force side, as MSDL (SISO-STD-007-2008) and C2SIM (SISO-STD-019-2020) have them: blue, red, a neutral
/// population, exercise control's own cell.
/// </summary>
[Parts(Part.World)]
public sealed record Side : Item
{
    /// <summary>The side's name.</summary>
    public required string Name { get; init; }

    /// <summary>What the side is, in a sentence or two.</summary>
    public string? Description { get; init; }

    /// <summary>How the side regards each other side.</summary>
    public IReadOnlyList<SideRelation> Relations { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(Relations.Select(r => r.SideId));
}

/// <summary>How one side regards another, as MSDL's hostility codes do.</summary>
public sealed record SideRelation
{
    /// <summary>The other side.</summary>
    public required string SideId { get; init; }

    /// <summary>How this side regards it.</summary>
    public required Stance Stance { get; init; }
}

/// <summary>How one side regards another.</summary>
public enum Stance { Friendly, Neutral, Hostile, Unknown }

/// <summary>
/// An organization: a bank, a ministry, a unit, a news outlet. The Cyber DEM has no organization; GHOSTS needs one for
/// every persona's employer.
/// </summary>
[Parts(Part.World)]
public sealed record Organization : Item
{
    /// <summary>The organization's name.</summary>
    public required string Name { get; init; }

    /// <summary>What the organization is and does.</summary>
    public string? Description { get; init; }

    /// <summary>Its sector or type, such as banking, government or military unit.</summary>
    public string? Sector { get; init; }

    /// <summary>The side it is on.</summary>
    public string? SideId { get; init; }

    /// <summary>The organization it is part of.</summary>
    public string? ParentId { get; init; }

    /// <summary>Where it is.</summary>
    public Location? Location { get; init; }

    /// <inheritdoc />
    public override IEnumerable<string> References() =>
        base.References().Concat(new[] { SideId, ParentId }.OfType<string>());
}

/// <summary>
/// What every Cyber DEM object has (CyberObject: ObjectID, Name, Description, RelatedObjects). The item's id is the
/// Cyber DEM ObjectID.
/// </summary>
public abstract record CyberObject : Item
{
    /// <summary>The object's name, in the Cyber DEM's sense for its class.</summary>
    public required string Name { get; init; }

    /// <summary>What the object is.</summary>
    public string? Description { get; init; }

    /// <summary>The other cyber objects this one is related to, and how.</summary>
    public IReadOnlyList<RelatedObject> RelatedObjects { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(RelatedObjects.Select(r => r.ObjectId));
}

/// <summary>A relationship between two cyber objects (the Cyber DEM's RelatedObjects).</summary>
public sealed record RelatedObject
{
    /// <summary>The related object.</summary>
    public required string ObjectId { get; init; }

    /// <summary>How the two relate, such as Administers, ResidesOn or Provides.</summary>
    [SisoEnumeration(695, "Cyber Relationship Type")]
    public required string Relationship { get; init; }

    /// <summary>What this object may do on the related one (the Cyber DEM's RelatedObjectPrivileges).</summary>
    public IReadOnlyList<string> Privileges { get; init; } = [];
}

/// <summary>A network or segment (the Cyber DEM's Network).</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Network : CyberObject
{
    /// <summary>The network's address range in CIDR form, such as 10.1.20.0/24. The Cyber DEM records it as the Network's name and mask.</summary>
    public string? Cidr { get; init; }

    /// <summary>The network protocol.</summary>
    [SisoEnumeration(690, "Cyber Network Protocol Type")]
    public string? Protocol { get; init; }
}

/// <summary>A link between devices, when an exercise needs its conditions (the Cyber DEM's NetworkLink).</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record NetworkLink : CyberObject
{
    /// <summary>The data link protocol.</summary>
    [SisoEnumeration(680, "Cyber Data Link Protocol Type")]
    public string? DataLinkProtocol { get; init; }

    /// <summary>The physical layer.</summary>
    [SisoEnumeration(693, "Cyber Physical Layer Type")]
    public string? PhysicalLayer { get; init; }

    /// <summary>Bandwidth, in bits per second.</summary>
    public long? Bandwidth { get; init; }

    /// <summary>Latency, in milliseconds.</summary>
    public double? Latency { get; init; }

    /// <summary>Jitter, in milliseconds.</summary>
    public double? Jitter { get; init; }
}

/// <summary>A host, server, router or other device (the Cyber DEM's Device).</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Device : CyberObject
{
    /// <summary>What kinds of device it is, such as ComputerNode or Networking.</summary>
    [SisoEnumeration(683, "Cyber Device Type")]
    public IReadOnlyList<string> DeviceTypes { get; init; } = [];

    /// <summary>Whether the device is virtual.</summary>
    public bool? IsVirtual { get; init; }

    /// <summary>The device's purpose, such as domain controller.</summary>
    public string? Role { get; init; }

    /// <summary>An identifier for the device, such as a serial or asset number.</summary>
    public string? DeviceIdentifier { get; init; }

    /// <summary>The device's connections to networks.</summary>
    public IReadOnlyList<NetworkInterface> NetworkInterfaces { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() =>
        base.References().Concat(NetworkInterfaces.Select(n => n.RelatedObjectId));
}

/// <summary>A device's connection to a network (the Cyber DEM's NetworkInterfaceStruct).</summary>
public sealed record NetworkInterface
{
    /// <summary>The network or network link.</summary>
    public required string RelatedObjectId { get; init; }

    /// <summary>The device's hostname on that network.</summary>
    public string? Name { get; init; }

    /// <summary>The device's IP address on that network.</summary>
    public string? Address { get; init; }
}

/// <summary>An operating system, related to the device it runs on (the Cyber DEM's OperatingSystem).</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record OperatingSystem : CyberObject
{
    /// <summary>The operating system type, such as MicrosoftWindows or UNIX-Linux.</summary>
    [SisoEnumeration(691, "Cyber Operating System Type")]
    public required string OsType { get; init; }

    /// <summary>The version, such as Windows Server 2022 or Ubuntu 24.04.</summary>
    public string? Version { get; init; }
}

/// <summary>A service: DNS, mail, a website, a database (the Cyber DEM's Service). A social media platform is a service.</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Service : CyberObject
{
    /// <summary>The service type, such as DNS, Email, Web or Database.</summary>
    [SisoEnumeration(697, "Cyber Service Type")]
    public required string ServiceType { get; init; }

    /// <summary>Where the service is reached: a URL, a host and port, an address.</summary>
    public string? Address { get; init; }
}

/// <summary>A program on a device (the Cyber DEM's Application).</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Application : CyberObject
{
    /// <summary>The version.</summary>
    public string? Version { get; init; }

    /// <summary>The company that makes it.</summary>
    public string? Company { get; init; }
}

/// <summary>A group of objects that work together, such as a SCADA, C2 or ICS system (the Cyber DEM's System).</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record CyberSystem : CyberObject
{
    /// <summary>The system type, such as SCADA, C2 or ICS.</summary>
    [SisoEnumeration(698, "Cyber System Type")]
    public required string SystemType { get; init; }
}

/// <summary>Files, messages, records and credentials (the Cyber DEM's Data). A persona's passwords are Data of type Credentials.</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Data : CyberObject
{
    /// <summary>The data type, such as Credentials.</summary>
    [SisoEnumeration(682, "Cyber Data Type")]
    public string? DataType { get; init; }

    /// <summary>The data's state, such as Intact, Compromised or Corrupted.</summary>
    [SisoEnumeration(681, "Cyber Data Status Type")]
    public string? Status { get; init; }

    /// <summary>How sensitive the data is, such as PII.</summary>
    [SisoEnumeration(696, "Cyber Sensitivity Type")]
    public string? Sensitivity { get; init; }

    /// <summary>Whether the data is encrypted.</summary>
    public bool? Encrypted { get; init; }

    /// <summary>How it is encrypted.</summary>
    [SisoEnumeration(684, "Cyber Encryption Type")]
    public string? EncryptionType { get; init; }
}

/// <summary>
/// A person in cyberspace (the Cyber DEM's Persona), with what GHOSTS needs: an account on each service, an
/// organization and role, where the persona came from, and its traits. A persona's devices and services are related
/// objects with the Uses relationship.
/// </summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Persona : CyberObject
{
    /// <summary>The persona's accounts, one for each service it uses.</summary>
    public IReadOnlyList<Account> Accounts { get; init; } = [];

    /// <summary>The organization the persona works for.</summary>
    public string? OrganizationId { get; init; }

    /// <summary>The persona's role there.</summary>
    public string? Role { get; init; }

    /// <summary>The pool the persona was generated from.</summary>
    public string? PoolId { get; init; }

    /// <summary>Where the persona came from, which tells a reviewer what to check.</summary>
    public required PersonaOrigin Origin { get; init; }

    /// <summary>Optional traits, such as motivation or health. Sensitive ones carry their sensitivity.</summary>
    public IReadOnlyList<Trait> Traits { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References()
        .Concat(Accounts.Select(a => a.ServiceId))
        .Concat(new[] { OrganizationId, PoolId }.OfType<string>());
}

/// <summary>A persona's account on one service. Credentials are never here: they are Data of type Credentials.</summary>
public sealed record Account
{
    /// <summary>The service.</summary>
    public required string ServiceId { get; init; }

    /// <summary>The username, or email address, that every post and message on the service is attributed to.</summary>
    public required string Username { get; init; }

    /// <summary>The name the service shows, if it differs.</summary>
    public string? DisplayName { get; init; }
}

/// <summary>Where a persona came from.</summary>
public enum PersonaOrigin
{
    /// <summary>A person wrote it.</summary>
    Authored,

    /// <summary>Software generated it, such as GHOSTS's Animator.</summary>
    Generated,

    /// <summary>It is modelled on a real person, which matters for privacy.</summary>
    ModelledOnRealPerson
}

/// <summary>One trait of a persona.</summary>
public sealed record Trait
{
    /// <summary>The trait's name, such as motivation or insiderThreat.</summary>
    public required string Name { get; init; }

    /// <summary>The trait's value, in whatever form the trait needs.</summary>
    public required JsonElement Value { get; init; }

    /// <summary>How sensitive the trait is, such as PII; absent when it isn't.</summary>
    [SisoEnumeration(696, "Cyber Sensitivity Type")]
    public string? Sensitivity { get; init; }
}

/// <summary>
/// Many people, described once: "40 office staff on this segment". Scenarios are authored as pools; GHOSTS generates
/// the personas, and each names its pool.
/// </summary>
[Parts(Part.World)]
public sealed record PersonaPool : Item
{
    /// <summary>The pool's name.</summary>
    public required string Name { get; init; }

    /// <summary>Who the people are and how they behave.</summary>
    public string? Description { get; init; }

    /// <summary>Their role.</summary>
    public string? Role { get; init; }

    /// <summary>How many people.</summary>
    public required int Count { get; init; }

    /// <summary>The organization they work for.</summary>
    public string? OrganizationId { get; init; }

    /// <summary>The networks, devices and services they use.</summary>
    public IReadOnlyList<string> Uses { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() =>
        base.References().Concat(Uses).Concat(new[] { OrganizationId }.OfType<string>());
}

/// <summary>A weakness planted or present in an asset. The Cyber DEM has no vulnerability object.</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Vulnerability : Item
{
    /// <summary>The device, service, application or system that has it.</summary>
    public required string AssetId { get; init; }

    /// <summary>What the weakness is.</summary>
    public required string Description { get; init; }

    /// <summary>Its CVE id, if it has one.</summary>
    public string? Cve { get; init; }

    /// <summary>The ATT&amp;CK techniques that exploit it, as supporting detail.</summary>
    public IReadOnlyList<string> MitreTechniqueIds { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Append(AssetId);
}

/// <summary>A defense in place before the event starts. A defense taken during play is a CyberActivity of class CyberDefend.</summary>
[Parts(Part.World, Part.AsBuilt)]
public sealed record Defense : Item
{
    /// <summary>What the defense is.</summary>
    public required string Description { get; init; }

    /// <summary>The assets it protects.</summary>
    public IReadOnlyList<string> AssetIds { get; init; } = [];

    /// <summary>The ATT&amp;CK mitigations it carries out, as supporting detail.</summary>
    public IReadOnlyList<string> MitreMitigationIds { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(AssetIds);
}

/// <summary>
/// A statement about the world: background, a situation, the information environment, a name. Ground truth is a fact
/// with excon visibility.
/// </summary>
[Parts(Part.World)]
public sealed record Fact : Item
{
    /// <summary>What the fact is about, such as background, situation, informationEnvironment or groundTruth.</summary>
    public string? Topic { get; init; }

    /// <summary>The fact.</summary>
    public required string Text { get; init; }

    /// <summary>The items the fact is about.</summary>
    public IReadOnlyList<string> About { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(About);
}
