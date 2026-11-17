# Chronicle.Client

The way a .NET app joins a Chronicle. Subscribe to the parts you need, publish what you did, and leave the plumbing to
the library: connecting, reconnecting, resuming, retrying, audiences and the spec's types. What your app *does* with
an entry is your code, not ours.

It depends on `Chronicle`, the spec library, whose types (`Entry`, `Item` and its kinds, `Subscription`) it speaks.
Apps in other languages use the API directly; see [the API](../README.md#the-api).

## Two Ways In

**A handler.** Write one class; the library runs it on every entry, in order, in the background.

```csharp
// Program.cs
builder.Services.AddChronicleClient(builder.Configuration);                 // Chronicle:Url, Chronicle:App
builder.Services.AddSingleton<ICursorStore>(new FileCursorStore("cursors")); // resume after a restart
builder.Services.AddChronicleSubscriber<NpcHandler>("office-staff", o =>
{
    o.ChronicleId = "harbor-ttx";
    o.Audience = Audience.Participant;          // who we read as
    o.Viewers = ["office-staff"];                // who we speak for: widens what we see by knownBy
    o.Parts = [Part.World, Part.AsRun];
    o.SubscriberId = "ghosts-office-staff";      // optional: show up on the Chronicle UI's Subscribers page
});

// NpcHandler.cs: your app's own logic
public sealed class NpcHandler(ChronicleClient chronicle) : IChronicleHandler
{
    public async Task HandleAsync(Entry entry, CancellationToken ct)
    {
        if (entry.Item is InjectDelivery inject)
            await chronicle.AddAsync(entry.ChronicleId, Part.AsRun, new Observation { /* … */ }, ct: ct);
    }
}
```

**A loop.** When the app decides at run time what to subscribe to (which exercise, which personas), use the client
directly:

```csharp
var chronicle = ChronicleClient.Create("http://localhost:5285", app: "ghosts");   // or inject it

await foreach (var entry in chronicle.SubscribeAsync(new Subscription
{
    ChronicleId = "harbor-ttx",
    Audience = Audience.Participant,
    Viewers = ["persona-dana"],
    Parts = [Part.World, Part.AsRun],
    AfterSequence = lastHandled          // 0 replays the whole history
}, ct))
{
    await Handle(entry);
    lastHandled = entry.Sequence!.Value;
}
```

[`examples/GhostsSubscriber`](../examples/GhostsSubscriber) is a working app of the first kind, shaped like GHOSTS.

## What You Can Do

| | |
|---|---|
| **Subscribe** | `SubscribeAsync` (typed entries) and `SubscribeRawAsync` (`RawEntry`: routing fields and the JSON, for kinds this library doesn't know yet) |
| **Read** | `GetAsync` / `GetJsonAsync` (the record as an audience sees it), `GetEntriesAsync`, `GetEntryAsync`, `ListAsync`, `GetFindingsAsync`, `ValidateAsync` |
| **Publish** | `AddAsync`, `ReviseAsync`, `RetractAsync` (they build the entry: id, time, author), or `PublishAsync` with an `Entry` you built |
| **Report activity** | `ReportActivityAsync`: the live feed (an NPC browsing, an email sent). With an `Entry`, it is notable and the entry is stored too |
| **Give a model context** | `GetContextAsync`: the subscriber's view as text a model can read, with how much it withholds |
| **Register** | `RegisterSubscriberAsync`, `UnregisterSubscriberAsync`, `GetSubscribersAsync`, `RunSubscriberAsync` |
| **Manage** | `CreateAsync`, `ArchiveAsync` |

## What It Promises, and What It Asks of You

- **Each entry once per subscription, in order.** A subscription reconnects whenever the connection drops and resumes
  after the last entry it delivered. The API sends a heartbeat every 15 seconds; a connection silent for
  `StreamIdleTimeout` (1 minute) is replaced.
- **At least once across restarts.** A hosted subscriber saves its cursor *after* the handler returns, so a crash
  mid-entry means that entry comes again. **Make handling an entry twice harmless.** For example, derive the ids of
  the items you publish from what you're reacting to, so the second publish is refused.
- **A handler that throws** gets the same entry again, after a backoff (`OnError = HandlerErrors.Retry`, the
  default), or is skipped past (`HandlerErrors.Skip`). Retry never loses an entry, but a bad entry stalls the
  subscription; the logs say which.
- **You see only what your audience may.** The API filters, not the library. Read as `participant` unless your app
  works for exercise control. Pass `Viewers` for the personas, pools or organizations you play.
- **Publishing is safe to retry.** The API stores an entry once, by its id, so the library retries calls that didn't
  get through or failed with a 5xx (`Retries`, 3). Activity reports aren't retried, so an activity isn't counted twice.
- **Conflicts are yours to decide.** A revise names the entry whose version it replaces. If someone revised the item
  since, you get a `ChronicleApiException` with `IsConflict` and `Code == "STALE"`: read the item again, and decide.
  `IsInvalid` (422) means the entry broke a spec rule; `Code` says which.
- **New kinds don't break you.** A typed subscription skips (and logs) an entry it can't read, such as a kind added
  to the spec after your build; set `UnreadableEntries = Throw` to stop instead, or subscribe raw to see everything.

## Configuration

`AddChronicleClient(builder.Configuration)` binds the `Chronicle` section:

| Key | Default | What |
|---|---|---|
| `Url` | `http://localhost:5285` | The Chronicle API |
| `App` | (none) | Your app's name, as authors and activity name it: `ghosts`, `pandora`. Needed to publish with the helpers |
| `Actor`, `Via` | (none), `app` | The default author's actor, and how it acts (`person`, `agent`, `app`) |
| `Retries`, `RetryDelay`, `MaxRetryDelay` | 3, 0.5 s, 30 s | Retries and reconnects, doubling the delay each time |
| `StreamIdleTimeout` | 1 minute | Reconnect a subscription that hears nothing, heartbeats included, for this long |
| `UnreadableEntries` | `Skip` | What a typed subscription does with an entry it can't read |

A hosted subscriber binds its own section (`AddChronicleSubscriber<T>(name, section)`): `ChronicleId`, `Audience`,
`Viewers`, `Parts`, `Kinds`, `OnError`, `SubscriberId`, `DisplayName`, `Role`. See the example's
[`appsettings.json`](../examples/GhostsSubscriber/appsettings.json).

**Authentication.** The Chronicle API has none yet. When it does, set `ConfigureRequest` to add the token to every
request; nothing else in your app changes.

## Working on It

```bash
dotnet test src/chronicle/Chronicle.Client.Tests    # against the real API, in memory
```
