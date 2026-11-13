// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Chronicle.Api.Models;

namespace Chronicle.Api.Services;

/// <summary>
/// The live side of each Chronicle: recent activity (in memory, the last <see cref="Keep"/> per Chronicle) and every open
/// live connection. A message goes only to listeners whose audience, or viewers, may see it.
/// </summary>
public sealed class LiveHub
{
    public const int Keep = 1000;

    /// <summary>A message for listeners, and who may hear it.</summary>
    /// <remarks><paramref name="Below"/>, when set, keeps it from that audience and up (they get another version).</remarks>
    public sealed record Message(string Json, Audience Visibility, string? Actor = null, Audience? Below = null);

    /// <summary>One open live connection: its audience, its viewers, and its queue.</summary>
    public sealed class Listener(Audience audience, IReadOnlyCollection<string> viewers)
    {
        public Audience Audience { get; } = audience;
        public IReadOnlyCollection<string> Viewers { get; } = viewers;
        public Channel<string> Queue { get; } = Channel.CreateBounded<string>(new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

        public bool Hears(Message m) =>
            (m.Below is null || Audience < m.Below) && (m.Visibility <= Audience || (m.Actor is not null && Viewers.Contains(m.Actor)));
    }

    private sealed class Room
    {
        public readonly LinkedList<Activity> Recent = new();
        public readonly List<Listener> Listeners = [];
    }

    private readonly ConcurrentDictionary<string, Room> rooms = new();

    private Room For(string chronicleId) => rooms.GetOrAdd(chronicleId, _ => new Room());

    /// <summary>Keeps an activity and sends it to every listener who may see it.</summary>
    public void Add(Activity activity)
    {
        var room = For(activity.ChronicleId);
        lock (room)
        {
            room.Recent.AddLast(activity);
            while (room.Recent.Count > Keep)
                room.Recent.RemoveFirst();
        }
        Send(activity.ChronicleId, new Message(Serialize(new { type = "activity", activity }), activity.Visibility, activity.Actor));
    }

    /// <summary>The newest activity <paramref name="audience"/> may see, oldest first.</summary>
    public IReadOnlyList<Activity> Recent(string chronicleId, Audience audience, IReadOnlyCollection<string> viewers, int limit = 200)
    {
        var room = For(chronicleId);
        lock (room)
            return [.. room.Recent.Where(a => a.SeenBy(audience, viewers)).TakeLast(Math.Clamp(limit, 1, Keep))];
    }

    /// <summary>Sends a message to every listener on a Chronicle who may hear it.</summary>
    public void Send(string chronicleId, Message message)
    {
        var room = For(chronicleId);
        lock (room)
            foreach (var listener in room.Listeners)
                if (listener.Hears(message))
                    listener.Queue.Writer.TryWrite(message.Json);
    }

    /// <summary>Starts listening; dispose the result to stop.</summary>
    public (Listener Listener, IDisposable Stop) Listen(string chronicleId, Audience audience, IReadOnlyCollection<string> viewers)
    {
        var room = For(chronicleId);
        var listener = new Listener(audience, viewers);
        lock (room)
            room.Listeners.Add(listener);
        return (listener, new Stop(() =>
        {
            lock (room)
                room.Listeners.Remove(listener);
            listener.Queue.Writer.TryComplete();
        }));
    }

    /// <summary>Writes a live message as the spec's JSON writes it.</summary>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ChronicleJson.Compact);

    private sealed class Stop(Action stop) : IDisposable
    {
        public void Dispose() => stop();
    }
}
