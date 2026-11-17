// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Data.Sqlite;

namespace Chronicle.Api.Services;

/// <summary>An entry as stored, with the item version it is about: the new version, or for a retract the one withdrawn.</summary>
public sealed record Stored(Entry Entry, Item Subject);

/// <summary>
/// Every Chronicle, in memory, and its entries in SQLite. A Chronicle's history is its only durable state: the parts
/// are rebuilt from it on load. Appends to one Chronicle run one at a time; each stored entry goes to its subscribers.
/// </summary>
public sealed class ChronicleStore(string connectionString, ILogger<ChronicleStore> log)
{
    private sealed class Slot(ChronicleRecord record)
    {
        public readonly SemaphoreSlim Lock = new(1, 1);
        public ChronicleRecord Record = record;
        public bool Archived;
        public readonly List<Channel<Stored>> Subscribers = [];
    }

    private readonly ConcurrentDictionary<string, Slot> slots = new();

    /// <summary>Creates the tables if they're missing and loads every Chronicle.</summary>
    public void Load()
    {
        using var db = Open();
        Execute(db, """
            CREATE TABLE IF NOT EXISTS chronicles (id TEXT PRIMARY KEY, header TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS entries (
                chronicle_id TEXT NOT NULL REFERENCES chronicles(id),
                sequence INTEGER NOT NULL,
                id TEXT NOT NULL UNIQUE,
                entry TEXT NOT NULL,
                PRIMARY KEY (chronicle_id, sequence));
            """);
        if (!HasColumn(db, "chronicles", "archived"))
            Execute(db, "ALTER TABLE chronicles ADD COLUMN archived INTEGER NOT NULL DEFAULT 0");

        var headers = new List<(ChronicleRecord Header, bool Archived)>();
        using (var read = db.CreateCommand())
        {
            read.CommandText = "SELECT header, archived FROM chronicles";
            using var rows = read.ExecuteReader();
            while (rows.Read())
                headers.Add((ChronicleJson.Deserialize<ChronicleRecord>(rows.GetString(0)), rows.GetInt64(1) != 0));
        }

        foreach (var (header, archived) in headers)
        {
            var history = new List<Entry>();
            using var read = db.CreateCommand();
            read.CommandText = "SELECT entry FROM entries WHERE chronicle_id = $id ORDER BY sequence";
            read.Parameters.AddWithValue("$id", header.Id);
            using var rows = read.ExecuteReader();
            while (rows.Read())
                history.Add(ChronicleJson.Deserialize<Entry>(rows.GetString(0)));
            slots[header.Id] = new Slot(Projection.Rebuild(header with { History = history })) { Archived = archived };
        }
        log.LogInformation("Loaded {Count} Chronicles", slots.Count);
    }

    /// <summary>Every Chronicle, newest first, archived ones included, each with whether it's archived.</summary>
    public IReadOnlyList<(ChronicleRecord Record, bool Archived)> All() =>
        [.. slots.Values.Select(s => (s.Record, s.Archived)).OrderByDescending(r => r.Record.Created)];

    /// <summary>
    /// Archives a Chronicle, which hides it from the list, or brings it back. Nothing else changes: it is still read,
    /// published to and subscribed to as before. False if there is no such Chronicle.
    /// </summary>
    public async Task<bool> Archive(string id, bool archived, CancellationToken ct)
    {
        if (!slots.TryGetValue(id, out var slot))
            return false;
        await slot.Lock.WaitAsync(ct);
        try
        {
            await using var db = Open();
            await using var update = db.CreateCommand();
            update.CommandText = "UPDATE chronicles SET archived = $a WHERE id = $id";
            update.Parameters.AddWithValue("$a", archived ? 1 : 0);
            update.Parameters.AddWithValue("$id", id);
            await update.ExecuteNonQueryAsync(ct);
            slot.Archived = archived;
            return true;
        }
        finally
        {
            slot.Lock.Release();
        }
    }

    /// <summary>One Chronicle, in full; null if there is none.</summary>
    public ChronicleRecord? Get(string id) => slots.TryGetValue(id, out var slot) ? slot.Record : null;

    /// <summary>
    /// Stores a new Chronicle: an empty one, or an existing record imported with its history. Returns the findings
    /// that stopped it, or none.
    /// </summary>
    public async Task<IReadOnlyList<Finding>> Create(ChronicleRecord record, CancellationToken ct)
    {
        var errors = Validation.Check(record).Where(f => f.Severity == Severity.Error).ToList();
        if (record.History.Count == 0 && record.Items.Any())
            errors.Add(new(Severity.Error, "HISTORY_MISSING", "A record with items is imported with the history that made them."));
        if (errors.Count > 0)
            return errors;

        var slot = new Slot(record);
        if (!slots.TryAdd(record.Id, slot))
            return [new(Severity.Error, "CHRONICLE_EXISTS", $"Chronicle '{record.Id}' already exists.")];

        try
        {
            await using var db = Open();
            await using var tx = (SqliteTransaction)await db.BeginTransactionAsync(ct);
            await using (var insert = db.CreateCommand())
            {
                insert.CommandText = "INSERT INTO chronicles (id, header) VALUES ($id, $header)";
                insert.Parameters.AddWithValue("$id", record.Id);
                insert.Parameters.AddWithValue("$header", ChronicleJson.Serialize(Header(record), indented: false));
                await insert.ExecuteNonQueryAsync(ct);
            }
            foreach (var entry in record.History)
                await Insert(db, entry, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            slots.TryRemove(record.Id, out _);
            throw;
        }
        return [];
    }

    /// <summary>
    /// Stores one entry and tells its subscribers. An entry already stored (the same id, posted again) returns the stored
    /// copy and changes nothing, so a publisher can retry safely.
    /// </summary>
    /// <returns>The stored entry, and whether it was new.</returns>
    /// <exception cref="KeyNotFoundException">There is no such Chronicle.</exception>
    /// <exception cref="ChronicleException">The entry breaks a rule.</exception>
    public async Task<(Entry Entry, bool Created)> Append(string chronicleId, Entry entry, CancellationToken ct)
    {
        if (!slots.TryGetValue(chronicleId, out var slot))
            throw new KeyNotFoundException(chronicleId);

        await slot.Lock.WaitAsync(ct);
        try
        {
            var before = slot.Record;
            if (before.History.FirstOrDefault(e => e.Id == entry.Id) is { } already)
                return (already, false);

            var after = Projection.Append(before, entry);
            var stored = after.History[^1];
            var subject = stored.Item ?? before.Items.First(i => i.Item.Id == stored.ItemId).Item;

            await using (var db = Open())
                await Insert(db, stored, ct);
            // Under the subscribers' lock, so a new subscriber sees the entry either in its record or on its channel.
            lock (slot.Subscribers)
            {
                slot.Record = after;
                foreach (var subscriber in slot.Subscribers)
                    subscriber.Writer.TryWrite(new Stored(stored, subject));
            }
            return (stored, true);
        }
        finally
        {
            slot.Lock.Release();
        }
    }

    /// <summary>
    /// Starts hearing a Chronicle's new entries. Returns the record as it stood when hearing began, so the caller can
    /// replay what came before without a gap; null if there is no such Chronicle.
    /// </summary>
    public (ChronicleRecord Record, ChannelReader<Stored> Reader, IDisposable Stop)? Subscribe(string chronicleId)
    {
        if (!slots.TryGetValue(chronicleId, out var slot))
            return null;
        var channel = Channel.CreateUnbounded<Stored>(new() { SingleReader = true });
        ChronicleRecord record;
        lock (slot.Subscribers)
        {
            slot.Subscribers.Add(channel);
            record = slot.Record;
        }
        return (record, channel.Reader, new Unsubscribe(() =>
        {
            lock (slot.Subscribers)
                slot.Subscribers.Remove(channel);
            channel.Writer.TryComplete();
        }));
    }

    private static ChronicleRecord Header(ChronicleRecord record) =>
        record with { World = [], Intent = [], AsBuilt = [], AsRun = [], Lessons = [], History = [] };

    private static async Task Insert(SqliteConnection db, Entry entry, CancellationToken ct)
    {
        await using var insert = db.CreateCommand();
        insert.CommandText = "INSERT INTO entries (chronicle_id, sequence, id, entry) VALUES ($c, $s, $id, $e)";
        insert.Parameters.AddWithValue("$c", entry.ChronicleId);
        insert.Parameters.AddWithValue("$s", entry.Sequence!.Value);
        insert.Parameters.AddWithValue("$id", entry.Id.ToString());
        insert.Parameters.AddWithValue("$e", ChronicleJson.Serialize(entry, indented: false));
        await insert.ExecuteNonQueryAsync(ct);
    }

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(connectionString);
        db.Open();
        return db;
    }

    private static bool HasColumn(SqliteConnection db, string table, string column)
    {
        using var command = db.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $column";
        command.Parameters.AddWithValue("$column", column);
        return (long)command.ExecuteScalar()! > 0;
    }

    private static void Execute(SqliteConnection db, string sql)
    {
        using var command = db.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private sealed class Unsubscribe(Action stop) : IDisposable
    {
        public void Dispose() => stop();
    }
}
