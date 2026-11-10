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
    public void An_id_is_never_reused()
    {
        var add = Entry.Add("t", Part.World, Fact("f1"), Me, At);
        var r = Projection.Append(Empty(), add);
        r = Projection.Append(r, Entry.Retract("t", Part.World, "fact", "f1", add.Id, Me, At));
        var again = Entry.Add("t", Part.World, Fact("f1"), Me, At);
        Assert.Equal("ID_TAKEN", Assert.Throws<ChronicleException>(() => Projection.Append(r, again)).Code);
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
