// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

/// <summary>
/// Marks a property whose values come from a SISO-REF-010 enumeration table, written as SISO-REF-010 writes them
/// (the Cyber DEM's JSON form maps every enumeration to its string; BONES §8.4). The JSON Schema carries the table as
/// <c>x-siso-ref-010</c>, so a validator holding SISO-REF-010.xml can check the values.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SisoEnumerationAttribute(int uid, string table) : Attribute
{
    /// <summary>The table's UID in SISO-REF-010.</summary>
    public int Uid { get; } = uid;

    /// <summary>The table's name in SISO-REF-010.</summary>
    public string Table { get; } = table;
}

/// <summary>
/// Names from the Cyber Data Exchange Model (SISO-STD-025-2023). These are the values the reference material quotes;
/// the full tables are in SISO-REF-010-2026, UIDs 677 to 698.
/// </summary>
public static class CyberDem
{
    /// <summary>The Cyber DEM's event classes (SISO-STD-025.2-2023). A Chronicle names the most specific one that fits.</summary>
    public static class EventClass
    {
        // CyberAction
        public const string CyberAttack = nameof(CyberAttack);
        public const string PhishingAttack = nameof(PhishingAttack);
        public const string ManipulationAttack = nameof(ManipulationAttack);
        public const string DataExfiltration = nameof(DataExfiltration);
        public const string CyberDefend = nameof(CyberDefend);
        public const string CyberRecon = nameof(CyberRecon);
        public const string CyberAdmin = nameof(CyberAdmin);
        public const string Detect = nameof(Detect);

        // CyberEffect
        public const string Deny = nameof(Deny);
        public const string Degrade = nameof(Degrade);
        public const string Disrupt = nameof(Disrupt);
        public const string Destroy = nameof(Destroy);
        public const string Manipulate = nameof(Manipulate);
        public const string DelayEffect = nameof(DelayEffect);
        public const string JitterEffect = nameof(JitterEffect);
        public const string DropEffect = nameof(DropEffect);
        public const string CPULoadEffect = nameof(CPULoadEffect);

        /// <summary>Every class above.</summary>
        public static readonly IReadOnlySet<string> All = new HashSet<string>
        {
            CyberAttack, PhishingAttack, ManipulationAttack, DataExfiltration, CyberDefend, CyberRecon, CyberAdmin, Detect,
            Deny, Degrade, Disrupt, Destroy, Manipulate, DelayEffect, JitterEffect, DropEffect, CPULoadEffect
        };
    }

    /// <summary>Cyber Cyber Event Phase Type, SISO-REF-010 UID 679.</summary>
    public static class Phase
    {
        public const string Start = nameof(Start);
        public const string Suspend = nameof(Suspend);
        public const string Continue = nameof(Continue);
        public const string ContinueWithChanges = nameof(ContinueWithChanges);
        public const string End = nameof(End);
        public const string Cancel = nameof(Cancel);
    }

    /// <summary>
    /// Cyber Relationship Type, SISO-REF-010 UID 695, as far as the reference material quotes it, and the
    /// <c>Uses</c>/<c>UsedBy</c> pair the GHOSTS team proposes. The pair isn't in SISO-REF-010 yet.
    /// </summary>
    public static class Relationship
    {
        public const string Administers = nameof(Administers);
        public const string ResidesOn = nameof(ResidesOn);
        public const string Provides = nameof(Provides);

        /// <summary>Proposed to SISO, not yet in SISO-REF-010.</summary>
        public const string Uses = nameof(Uses);

        /// <summary>Proposed to SISO, not yet in SISO-REF-010.</summary>
        public const string UsedBy = nameof(UsedBy);
    }
}
