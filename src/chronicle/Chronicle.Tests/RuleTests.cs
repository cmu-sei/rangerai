// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;

namespace Chronicle.Tests;

public class ProjectionTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly Author Me = new() { App = "test", Via = Via.Person };

    private static ChronicleRecord Empty() => ChronicleRecord.Start("t", "Test", EventKind.Training, At);

    private static Fact Fact(string id, string text = "x") => new() { Id = id, Text = text };

    [Fact]
    public void Add_revise_retract_make_the_parts_and_keep_the_history()
    {
        var add = Entry.Add("t", Part.World, Fact("f1", "first"), Me, At);
        var r = Projection.Append(Empty(), add);
        var revise = Entry.Revise("t", Part.World, Fact("f1", "second"), add.Id, Me, At);
        r = Projection.Append(r, revise);
        Assert.Equal("second", Assert.IsType<Fact>(Assert.Single(r.World)).Text);

        r = Projection.Append(r, Entry.Retract("t", Part.World, "fact", "f1", revise.Id, Me, At));
        Assert.Empty(r.World);
        Assert.Equal([1L, 2L, 3L], r.History.Select(e => e.Sequence!.Value));
    }

    [Fact]
    public void A_revise_of_a_replaced_version_is_stale()
    {
        var add = Entry.Add("t", Part.World, Fact("f1"), Me, At);
        var r = Projection.Append(Empty(), add);
        r = Projection.Append(r, Entry.Revise("t", Part.World, Fact("f1", "mine"), add.Id, Me, At));

        var theirs = Entry.Revise("t", Part.World, Fact("f1", "theirs"), add.Id, Me, At);
        Assert.Equal("STALE", Assert.Throws<ChronicleException>(() => Projection.Append(r, theirs)).Code);
    }

    [Fact]
    public void An_id_in_use_is_taken()
    {
        var r = Projection.Append(Empty(), Entry.Add("t", Part.World, Fact("f1"), Me, At));
        var again = Entry.Add("t", Part.World, Fact("f1"), Me, At);
        Assert.Equal("ID_TAKEN", Assert.Throws<ChronicleException>(() => Projection.Append(r, again)).Code);
    }

    [Fact]
    public void A_retracted_id_can_be_added_again_and_the_history_keeps_both()
    {
        var add = Entry.Add("t", Part.World, Fact("f1", "first"), Me, At);
        var r = Projection.Append(Empty(), add);
        r = Projection.Append(r, Entry.Retract("t", Part.World, "fact", "f1", add.Id, Me, At));

        var again = Entry.Add("t", Part.World, Fact("f1", "second"), Me, At);
        r = Projection.Append(r, again);
        Assert.Equal("second", Assert.IsType<Fact>(Assert.Single(r.World)).Text);
        Assert.Equal([Operation.Add, Operation.Retract, Operation.Add], r.History.Select(e => e.Op));
        Assert.Empty(Validation.Check(r));

        // The new item is revised from the entry that added it, not from the old one's.
        var stale = Entry.Revise("t", Part.World, Fact("f1", "third"), add.Id, Me, At);
        Assert.Equal("STALE", Assert.Throws<ChronicleException>(() => Projection.Append(r, stale)).Code);
        r = Projection.Append(r, Entry.Revise("t", Part.World, Fact("f1", "third"), again.Id, Me, At));
        Assert.Equal("third", Assert.IsType<Fact>(Assert.Single(r.World)).Text);
    }

    [Fact]
    public void A_list_numbered_by_position_can_lose_its_first_item()
    {
        // Scenario Builder numbers objectives O1, O2, O3 by position. Deleting the first renumbers the rest.
        var r = Empty();
        var last = new Dictionary<string, Entry>();
        void Publish(Entry e) { r = Projection.Append(r, e); last[e.ItemId] = e; }
        Objective O(int n, string text) => new() { Id = $"O{n}", Text = text };

        Publish(Entry.Add("t", Part.Intent, O(1, "a"), Me, At));
        Publish(Entry.Add("t", Part.Intent, O(2, "b"), Me, At));
        Publish(Entry.Add("t", Part.Intent, O(3, "c"), Me, At));

        Publish(Entry.Revise("t", Part.Intent, O(1, "b"), last["O1"].Id, Me, At));
        Publish(Entry.Revise("t", Part.Intent, O(2, "c"), last["O2"].Id, Me, At));
        Publish(Entry.Retract("t", Part.Intent, "objective", "O3", last["O3"].Id, Me, At));
        Assert.Equal(["b", "c"], r.Intent.Cast<Objective>().Select(o => o.Text));

        Publish(Entry.Add("t", Part.Intent, O(3, "d"), Me, At));
        Assert.Equal(["O1", "O2", "O3"], r.Intent.Select(i => i.Id));
        Assert.Equal(7, r.History.Count);
    }

    [Theory]
    [InlineData(Part.Lessons, "KIND_NOT_IN_PART")]
    public void A_kind_stays_in_its_parts(Part part, string code)
    {
        var entry = Entry.Add("t", part, Fact("f1"), Me, At);
        Assert.Equal(code, Assert.Throws<ChronicleException>(() => Projection.Append(Empty(), entry)).Code);
    }

    [Fact]
    public void An_item_never_changes_kind()
    {
        var add = Entry.Add("t", Part.Intent, new Rule { Id = "x", Text = "r" }, Me, At);
        var r = Projection.Append(Empty(), add);
        var asTerm = Entry.Revise("t", Part.Intent, new Term { Id = "x", Text = "r", Usage = TermUsage.Fixed }, add.Id, Me, At);
        Assert.Equal("KIND_CHANGED", Assert.Throws<ChronicleException>(() => Projection.Append(r, asTerm)).Code);
    }

    [Fact]
    public void An_entry_for_another_chronicle_is_refused()
    {
        var entry = Entry.Add("other", Part.World, Fact("f1"), Me, At);
        Assert.Equal("WRONG_CHRONICLE", Assert.Throws<ChronicleException>(() => Projection.Append(Empty(), entry)).Code);
    }
}

public class AudienceViewTests
{
    private static readonly ChronicleRecord Sample = HarborSample.Build();

    [Fact]
    public void Participants_never_see_ground_truth_or_the_plan()
    {
        var view = AudienceView.For(Sample, Audience.Participant);
        Assert.DoesNotContain(view.Items, i => i.Item.Id is "patient-zero" or "pe-phish" or "vpn-cve" or "red");
        Assert.All(view.Items, i => Assert.Equal(Audience.Participant, i.Item.Visibility));
        Assert.DoesNotContain(view.History, e => e.ItemId == "patient-zero");
    }

    [Fact]
    public void Exercise_control_sees_ground_truth_and_designers_see_everything()
    {
        Assert.Contains(AudienceView.For(Sample, Audience.Excon).World, i => i.Id == "patient-zero");
        Assert.Same(Sample, AudienceView.For(Sample, Audience.Designer));
    }

    [Fact]
    public void A_simulated_user_also_sees_what_its_pool_knows()
    {
        var dana = AudienceView.For(Sample, Audience.Participant, ["npc-dana", "office-staff", "port-authority"]);
        Assert.Contains(dana.World, i => i.Id == "harbor-master-away");
        Assert.DoesNotContain(dana.World, i => i.Id == "patient-zero");
        Assert.DoesNotContain(AudienceView.For(Sample, Audience.Participant).World, i => i.Id == "harbor-master-away");
    }

    [Fact]
    public void A_retraction_is_seen_by_whoever_saw_the_item()
    {
        var retract = Sample.History.Single(e => e.Op == Operation.Retract);
        Assert.Contains(AudienceView.For(Sample, Audience.Participant).History, e => e.Id == retract.Id);
    }

    [Fact]
    public void A_view_passes_validation_as_a_view()
    {
        var view = AudienceView.For(Sample, Audience.Participant);
        Assert.DoesNotContain(Validation.Check(view, isView: true), f => f.Severity == Severity.Error);
    }
}

public class ValidationTests
{
    private static readonly ChronicleRecord Sample = HarborSample.Build();

    [Fact]
    public void The_sample_is_valid() =>
        Assert.Empty(Validation.Check(Sample));

    [Fact]
    public void Duplicate_ids_and_dangling_references_are_errors()
    {
        var broken = Sample with
        {
            History = [],
            World = [.. Sample.World, new Fact { Id = "background", Text = "again" },
                     new Vulnerability { Id = "v2", AssetId = "no-such-host", Description = "d" }]
        };
        var codes = Validation.Check(broken).Select(f => f.Code).ToList();
        Assert.Contains("DUPLICATE_ID", codes);
        Assert.Contains("UNRESOLVED_REFERENCE", codes);
    }

    [Fact]
    public void A_reference_to_an_item_that_changed_since_is_a_warning_until_the_referrer_is_written_again()
    {
        var at = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
        var me = new Author { App = "test", Via = Via.Person };
        var objective = Entry.Add("t", Part.Intent, new Objective { Id = "O1", Text = "a" }, me, at);
        var point = Entry.Add("t", Part.Intent, new TeachingPoint { Id = "tp", Text = "p", ObjectiveIds = ["O1"] }, me, at);
        var r = Projection.Append(ChronicleRecord.Start("t", "Test", EventKind.Training, at), objective);
        r = Projection.Append(r, point);
        Assert.Empty(Validation.Check(r));

        var revised = Entry.Revise("t", Part.Intent, new Objective { Id = "O1", Text = "b" }, objective.Id, me, at);
        r = Projection.Append(r, revised);
        var finding = Assert.Single(Validation.Check(r));
        Assert.Equal(("REFERENCE_CHANGED", Severity.Warning, "tp"), (finding.Code, finding.Severity, finding.ItemId));

        // Writing the referrer again says it has seen the change.
        var seen = Projection.Append(r, Entry.Revise("t", Part.Intent, (TeachingPoint)point.Item!, point.Id, me, at));
        Assert.Empty(Validation.Check(seen));

        r = Projection.Append(r, Entry.Retract("t", Part.Intent, "objective", "O1", revised.Id, me, at));
        finding = Assert.Single(Validation.Check(r));
        Assert.Equal(("REFERENCE_RETRACTED", Severity.Warning, "tp"), (finding.Code, finding.Severity, finding.ItemId));
    }

    [Fact]
    public void Parts_that_disagree_with_the_history_are_an_error()
    {
        var edited = Sample with { World = Sample.World.Where(i => i.Id != "background").ToList() };
        Assert.Contains(Validation.Check(edited), f => f.Code == "PARTS_DIFFER_FROM_HISTORY");
    }

    [Fact]
    public void An_unknown_cyber_event_class_is_a_warning()
    {
        var planned = new PlannedEvent { Id = "pe-x", Title = "x", Offset = TimeSpan.Zero, Cyber = new() { Class = "Hack" } };
        var record = Sample with { History = [], Intent = [.. Sample.Intent, planned] };
        var finding = Assert.Single(Validation.Check(record));
        Assert.Equal(("UNKNOWN_EVENT_CLASS", Severity.Warning), (finding.Code, finding.Severity));
    }
}

public class JsonTests
{
    [Fact]
    public void The_sample_round_trips_exactly()
    {
        var json = ChronicleJson.Serialize(HarborSample.Build());
        Assert.Equal(json, ChronicleJson.Serialize(ChronicleJson.Deserialize<ChronicleRecord>(json)));
    }

    [Fact]
    public void An_item_with_no_visibility_stays_with_exercise_control()
    {
        var fact = ChronicleJson.Deserialize<Item>("""{ "id": "f", "text": "t", "kind": "fact" }""");
        Assert.Equal((Audience.Excon, Standing.Stated), (fact.Visibility, fact.Standing));
    }

    [Theory]
    [InlineData("""{ "kind": "fact", "id": "f", "text": "t", "colour": "blue" }""")]
    [InlineData("""{ "kind": "fact", "id": "f" }""")]
    [InlineData("""{ "kind": "fact", "id": "f", "text": "t", "visibility": 1 }""")]
    [InlineData("""{ "kind": "fact", "id": "f", "text": null }""")]
    public void Malformed_items_are_refused(string json) =>
        Assert.ThrowsAny<JsonException>(() => ChronicleJson.Deserialize<Item>(json));

    [Fact]
    public void Topics_route_by_chronicle_part_and_kind()
    {
        var entry = HarborSample.Build().History.First(e => e.Kind == "device");
        Assert.Equal("chronicle.harbor-ttx.world.device", Topics.For(entry));
        Assert.Equal("chronicle.harbor-ttx.asBuilt.*", Topics.For("harbor-ttx", Part.AsBuilt));
    }

    [Fact]
    public void A_subscription_picks_its_parts_kinds_and_position()
    {
        var history = HarborSample.Build().History;
        var ghosts = new Subscription { ChronicleId = "harbor-ttx", Parts = [Part.World, Part.Intent], Audience = Audience.Participant, AfterSequence = 3 };
        var wanted = history.Where(ghosts.Wants).ToList();
        Assert.All(wanted, e => Assert.True(e.Sequence > 3 && e.Part is Part.World or Part.Intent));
        Assert.NotEmpty(wanted);
    }
}
