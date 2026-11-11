// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;

namespace Chronicle.Tests;

/// <summary>
/// A small, fictional tabletop exercise, built the way apps would build it: one entry at a time, each by the app that
/// owns that part. It is written to samples/harbor-ttx.chronicle.json.
/// </summary>
public static class HarborSample
{
    public const string Id = "harbor-ttx";

    private static readonly DateTimeOffset T0 = new(2026, 11, 2, 14, 0, 0, TimeSpan.Zero);
    private static readonly Author Designer = new() { App = "scenario-builder", Actor = "designer@example.org", Via = Via.Person };
    private static readonly Author DesignerAgent = new() { App = "scenario-builder", Actor = "designer@example.org", Via = Via.Agent };
    private static readonly Author GraySpace = new() { App = "grayspace", Via = Via.App };
    private static readonly Author Ghosts = new() { App = "ghosts", Via = Via.App };
    private static readonly Author Pandora = new() { App = "pandora", Via = Via.App };
    private static readonly Author Controller = new() { App = "exercise-control", Actor = "controller@example.org", Via = Via.Person };
    private static readonly Author Analyst = new() { App = "after-action", Actor = "designer@example.org", Via = Via.Agent };

    public static ChronicleRecord Build()
    {
        var record = ChronicleRecord.Start(Id, "Harbor Ransomware Tabletop", EventKind.Exercise, T0) with
        {
            Description = "Port authority leaders respond to ransomware that reaches the cargo scheduling system."
        };
        var n = 0;
        var minutes = 0;
        var written = new Dictionary<string, Entry>();

        void Add(Part part, Item item, Author author, string? reason = null)
        {
            var entry = Entry.Add(Id, part, item, author, T0.AddMinutes(minutes++), reason) with { Id = Uuid(++n) };
            record = Projection.Append(record, entry);
            written[item.Id] = entry;
        }

        void Revise(Part part, Item item, Author author, string reason)
        {
            var entry = Entry.Revise(Id, part, item, written[item.Id].Id, author, T0.AddMinutes(minutes++), reason) with { Id = Uuid(++n) };
            record = Projection.Append(record, entry);
            written[item.Id] = entry;
        }

        void Retract(Part part, string itemId, Author author, string reason)
        {
            var last = written[itemId];
            var entry = Entry.Retract(Id, part, last.Kind, itemId, last.Id, author, T0.AddMinutes(minutes++), reason) with { Id = Uuid(++n) };
            record = Projection.Append(record, entry);
        }

        // 01 Imagine: Scenario Builder writes the world and the intent.
        Add(Part.World, new Side { Id = "blue", Name = "Port Authority", Visibility = Audience.Participant,
            Relations = [new() { SideId = "red", Stance = Stance.Hostile }] }, Designer);
        Add(Part.World, new Side { Id = "red", Name = "Tidewater Crew", Description = "A ransomware affiliate.",
            Relations = [new() { SideId = "blue", Stance = Stance.Hostile }] }, Designer);
        Add(Part.World, new Organization { Id = "port-authority", Name = "Bayport Port Authority", Sector = "transportation",
            SideId = "blue", Visibility = Audience.Participant, Location = new() { Name = "Bayport" } }, Designer);
        Add(Part.World, new Network { Id = "corp-net", Name = "Corporate", Cidr = "10.1.20.0/24", Protocol = "IPv4",
            Visibility = Audience.Participant }, Designer);
        Add(Part.World, new Device
        {
            Id = "mail-01", Name = "mail-01", DeviceTypes = ["ComputerNode"], IsVirtual = true, Role = "mail server",
            Visibility = Audience.Participant,
            NetworkInterfaces = [new() { RelatedObjectId = "corp-net", Name = "mail-01.bayport.example", Address = "10.1.20.10" }]
        }, Designer);
        Add(Part.World, new OperatingSystem { Id = "mail-01-os", Name = "Windows Server", OsType = "MicrosoftWindows",
            Version = "2022", Visibility = Audience.Participant,
            RelatedObjects = [new() { ObjectId = "mail-01", Relationship = CyberDem.Relationship.ResidesOn }] }, Designer);
        Add(Part.World, new Service { Id = "mail", Name = "Port Authority mail", ServiceType = "Email",
            Address = "mail.bayport.example", Visibility = Audience.Participant,
            RelatedObjects = [new() { ObjectId = "mail-01", Relationship = CyberDem.Relationship.ResidesOn }] }, Designer);
        Add(Part.World, new Service { Id = "harbor-talk", Name = "Harbor Talk", Description = "The town's social media site.",
            ServiceType = "Web", Visibility = Audience.Participant }, Designer);
        Add(Part.World, new PersonaPool { Id = "office-staff", Name = "Office staff", Role = "administrative staff", Count = 40,
            OrganizationId = "port-authority", Uses = ["corp-net", "mail", "harbor-talk"], Visibility = Audience.Participant,
            Description = "Busy, helpful, and quick to open attachments from the harbor master." }, DesignerAgent,
            "Proposed from the designer's staffing notes.");
        Add(Part.World, new Vulnerability { Id = "vpn-cve", AssetId = "mail-01", Cve = "CVE-2024-21762",
            Description = "Unpatched remote access on the mail host.", MitreTechniqueIds = ["T1190"] }, Designer);
        Add(Part.World, new Fact { Id = "background", Topic = "background", Visibility = Audience.Participant,
            Text = "Bayport handles a third of the region's container traffic." }, Designer);
        Add(Part.World, new Fact { Id = "patient-zero", Topic = "groundTruth", About = ["mail-01"],
            Text = "The crew got in through a phishing email to the harbor master's assistant." }, Designer);
        Add(Part.World, new Fact { Id = "harbor-master-away", Topic = "situation", KnownBy = ["office-staff"],
            Text = "The harbor master is on leave this week; staff know, participants must find out." }, Designer);

        Add(Part.Intent, new Summary { Id = "summary", Title = "Executive summary",
            Text = "Ransomware reaches Bayport's cargo scheduling through a phished assistant. Leaders must decide when to escalate, with the harbor master away." },
            DesignerAgent, "Drafted from the designer's answers.");
        Add(Part.Intent, new Objective { Id = "obj-escalate", Visibility = Audience.Participant,
            Text = "Leaders decide when to escalate to the national CERT.", Measures = ["Escalation decided within 30 minutes of the ransom note."] }, Designer);
        Add(Part.Intent, new TeachingPoint { Id = "tp-plan", Visibility = Audience.Participant, ObjectiveIds = ["obj-escalate"],
            Text = "An incident response plan only works if people know who owns each decision." }, Designer);
        Add(Part.Intent, new Rule { Id = "rule-scope", Category = "scope", Visibility = Audience.Participant,
            Text = "Ransom payment is in scope for discussion." }, Designer);
        Add(Part.Intent, new Term { Id = "term-cert", Text = "national CERT", Usage = TermUsage.Fixed, Visibility = Audience.Participant }, Designer);
        Add(Part.Intent, new PlannedEvent
        {
            Id = "pe-phish", Title = "Phishing email", Offset = TimeSpan.FromMinutes(10), ObjectiveIds = ["obj-escalate"],
            Cyber = new() { Class = CyberDem.EventClass.PhishingAttack, ActorIds = ["red"], TargetIds = ["mail"], MitreTechniqueIds = ["T1566.001"] },
            Inject = new() { Channel = "email", From = "harbor-master", To = ["office-staff"], Content = "Updated berth schedule attached." },
            ExpectedResponse = "Nobody notices yet."
        }, Designer);
        Add(Part.Intent, new PlannedEvent
        {
            Id = "pe-news", Title = "Local news picks it up", Offset = TimeSpan.FromMinutes(45), After = ["pe-phish"],
            Influence = new() { Action = InfluenceAction.Post, ActorId = "office-staff", ServiceId = "harbor-talk",
                Text = "Anyone else's email down at the port?" },
            Escalations = ["If nobody reacts, a reporter calls the communications lead."]
        }, Designer);

        // The designer moves the twist, and drops a rule.
        Revise(Part.World, (Fact)written["patient-zero"].Item! with
            { Text = "The crew got in through a phishing email to the harbor master's assistant, two weeks before play." },
            Designer, "The timeline needed the crew in place before the exercise starts.");
        Add(Part.Intent, new Rule { Id = "rule-press", Text = "No real press contact.", Visibility = Audience.Participant }, DesignerAgent);
        Retract(Part.Intent, "rule-press", Designer, "Covered by the venue's rules.");

        // 02 Build: the builders write what they made.
        Add(Part.AsBuilt, new Deployment { Id = "dep-grayspace", Builder = "grayspace", Reference = "harbor-ttx", Version = "3",
            State = DeploymentState.Ready, At = T0.AddDays(5), Builds = ["corp-net", "mail-01"] }, GraySpace);
        Add(Part.AsBuilt, new Realization { Id = "real-mail-01", Of = "mail-01", DeploymentId = "dep-grayspace",
            Identifiers = new Dictionary<string, string> { ["vmId"] = "vm-4471", ["hostname"] = "mail-01", ["address"] = "10.1.20.10" } }, GraySpace);
        Add(Part.AsBuilt, new Persona
        {
            Id = "npc-dana", Name = "dana.reyes@bayport.example", Origin = PersonaOrigin.Generated, PoolId = "office-staff",
            OrganizationId = "port-authority", Role = "assistant to the harbor master", Visibility = Audience.Participant,
            Accounts = [new() { ServiceId = "mail", Username = "dana.reyes@bayport.example" },
                        new() { ServiceId = "harbor-talk", Username = "dana_r", DisplayName = "Dana R." }],
            RelatedObjects = [new() { ObjectId = "mail-01", Relationship = CyberDem.Relationship.Uses }],
            Traits = [new() { Name = "motivation", Value = JsonSerializer.SerializeToElement("keep the berths on schedule") }]
        }, Ghosts);
        Add(Part.AsBuilt, new Content { Id = "article-traffic", Format = "article", Title = "Record month for Bayport",
            Body = "Container traffic rose for the fourth month running.", AttributedTo = "port-authority", ServiceId = "harbor-talk",
            Visibility = Audience.Participant }, Pandora);

        // 03 Run: what happened.
        Add(Part.AsRun, new Milestone { Id = "startex", At = T0.AddDays(7), Type = MilestoneType.Start, Visibility = Audience.Participant }, Controller);
        Add(Part.AsRun, new CyberActivity
        {
            Id = "act-phish", At = T0.AddDays(7).AddMinutes(11), PlannedEventId = "pe-phish", ObservedBy = "ghosts",
            Event = new() { Class = CyberDem.EventClass.PhishingAttack, Phase = CyberDem.Phase.Start, ActorIds = ["red"], TargetIds = ["npc-dana"] }
        }, Ghosts);
        Add(Part.AsRun, new InjectDelivery { Id = "inj-phish", At = T0.AddDays(7).AddMinutes(11), PlannedEventId = "pe-phish",
            To = ["npc-dana"], Note = "Opened at 14:12." }, Ghosts);
        Add(Part.AsRun, new ParticipantAction { Id = "dec-cert", At = T0.AddDays(7).AddMinutes(52), By = "Executive team",
            Text = "Escalated to the national CERT.", ObjectiveIds = ["obj-escalate"], Visibility = Audience.Participant }, Controller);
        Add(Part.AsRun, new Observation { Id = "obs-owner", At = T0.AddDays(7).AddMinutes(40), By = "controller@example.org",
            Text = "Nobody knew who could authorise taking mail offline.", ObjectiveIds = ["obj-escalate"] }, Controller);

        // 04 Remember: the lessons.
        Add(Part.Lessons, new Assessment { Id = "assess-escalate", ObjectiveId = "obj-escalate", Rating = Rating.PartiallyMet,
            Text = "They escalated, 22 minutes late.", Evidence = ["dec-cert", "obs-owner"], Visibility = Audience.Participant }, Analyst);
        Add(Part.Lessons, new Lesson { Id = "lesson-owner", Text = "Decision ownership for isolating systems is unclear.",
            Recommendation = "Name an owner for each containment decision in the plan.", For = "participants' organization",
            Evidence = ["obs-owner"], Visibility = Audience.Participant, Standing = Standing.Proposed }, Analyst);

        return record;
    }

    private static Guid Uuid(int n) => Guid.Parse($"0195f000-0000-7000-8000-{n:D12}");
}
