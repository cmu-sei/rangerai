// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Collections.Concurrent;
using System.Text.Json;
using Chronicle.Api.Models;
using Microsoft.Data.Sqlite;

namespace Chronicle.Api.Services;

/// <summary>
/// The subscribers of every Chronicle, kept in SQLite beside the entries so they survive a restart. What they did lately
/// (last seen, activity count) is kept in memory; what they published is written back.
/// </summary>
public sealed class SubscriberStore(string connectionString)
{
    private readonly ConcurrentDictionary<(string Chronicle, string Id), Subscriber> all = new();

    public void Load()
    {
        using var db = Open();
        using (var create = db.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS subscribers (
                    chronicle_id TEXT NOT NULL,
                    id TEXT NOT NULL,
                    body TEXT NOT NULL,
                    PRIMARY KEY (chronicle_id, id));
                """;
            create.ExecuteNonQuery();
        }
        using var read = db.CreateCommand();
        read.CommandText = "SELECT body FROM subscribers";
        using var rows = read.ExecuteReader();
        while (rows.Read())
        {
            var s = JsonSerializer.Deserialize<Subscriber>(rows.GetString(0), ChronicleJson.Compact)!;
            all[(s.ChronicleId, s.Id)] = s;
        }
    }

    public IReadOnlyList<Subscriber> For(string chronicleId) =>
        [.. all.Values.Where(s => s.ChronicleId == chronicleId).OrderBy(s => s.Registered)];

    public Subscriber? Get(string chronicleId, string id) => all.TryGetValue((chronicleId, id), out var s) ? s : null;

    /// <summary>Registers or replaces a subscriber, keeping what it has done so far.</summary>
    public Subscriber Put(string chronicleId, string id, SubscriberPut body)
    {
        var before = Get(chronicleId, id);
        var subscriber = new Subscriber
        {
            Id = id,
            ChronicleId = chronicleId,
            App = body.App,
            Name = body.Name,
            Role = body.Role,
            Audience = body.Audience,
            Viewers = body.Viewers,
            Parts = body.Parts,
            Kinds = body.Kinds,
            Model = body.Model,
            Agent = body.Agent,
            HasAgent = body.Agent is not null,
            Query = Query(chronicleId, body.Audience, body.Viewers, body.Parts),
            Registered = before?.Registered ?? DateTimeOffset.UtcNow,
            LastSeen = before?.LastSeen,
            LastActivity = before?.LastActivity,
            ActivityCount = before?.ActivityCount ?? 0,
            Published = before?.Published ?? 0,
            LastSequence = before?.LastSequence
        };
        all[(chronicleId, id)] = subscriber;
        Save(subscriber);
        return subscriber;
    }

    public bool Delete(string chronicleId, string id)
    {
        if (!all.TryRemove((chronicleId, id), out _))
            return false;
        using var db = Open();
        using var delete = db.CreateCommand();
        delete.CommandText = "DELETE FROM subscribers WHERE chronicle_id = $c AND id = $id";
        delete.Parameters.AddWithValue("$c", chronicleId);
        delete.Parameters.AddWithValue("$id", id);
        delete.ExecuteNonQuery();
        return true;
    }

    /// <summary>
    /// Counts an activity against its subscriber: the one it names, or else the one agentless subscriber of the reporting
    /// app (so GHOSTS can report without naming itself). Returns the subscriber, or null.
    /// </summary>
    public Subscriber? Saw(Activity activity)
    {
        var target = activity.SubscriberId is { } id
            ? Get(activity.ChronicleId, id)
            : For(activity.ChronicleId).FirstOrDefault(s => s.App == activity.Source && !s.HasAgent);
        if (target is null)
            return null;
        var updated = target with
        {
            LastSeen = activity.At,
            LastActivity = activity.Text ?? activity.Kind,
            ActivityCount = target.ActivityCount + 1,
            Published = target.Published + (activity.Sequence is null ? 0 : 1),
            LastSequence = activity.Sequence ?? target.LastSequence
        };
        all[(target.ChronicleId, target.Id)] = updated;
        if (activity.Sequence is not null)
            Save(updated);
        return updated;
    }

    /// <summary>The subscriber as <paramref name="audience"/> may see it: its agent's prompts stay with exercise control and designers.</summary>
    public static Subscriber ForAudience(Subscriber s, Audience audience) => audience >= Audience.Excon ? s : s with { Agent = null };

    /// <summary>The read a subscriber's view is, as a URL.</summary>
    public static string Query(string chronicleId, Audience audience, IReadOnlyList<string> viewers, IReadOnlyList<Part> parts)
    {
        var q = new List<string> { $"audience={JsonNamingPolicy.CamelCase.ConvertName(audience.ToString())}" };
        q.AddRange(viewers.Select(v => $"viewers={Uri.EscapeDataString(v)}"));
        q.AddRange(parts.Select(p => $"parts={JsonNamingPolicy.CamelCase.ConvertName(p.ToString())}"));
        return $"GET /api/chronicles/{chronicleId}?{string.Join('&', q)}";
    }

    private void Save(Subscriber s)
    {
        using var db = Open();
        using var upsert = db.CreateCommand();
        upsert.CommandText = "INSERT INTO subscribers (chronicle_id, id, body) VALUES ($c, $id, $b) ON CONFLICT (chronicle_id, id) DO UPDATE SET body = $b";
        upsert.Parameters.AddWithValue("$c", s.ChronicleId);
        upsert.Parameters.AddWithValue("$id", s.Id);
        upsert.Parameters.AddWithValue("$b", JsonSerializer.Serialize(s, ChronicleJson.Compact));
        upsert.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(connectionString);
        db.Open();
        return db;
    }
}
