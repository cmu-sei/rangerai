# Chronicle

One living record of an event, from the first idea to the last lesson. Apps publish entries to it and subscribe to
the parts they need. See the [concept brief](chronicle.html) for why, and the
[schema design](docs/specs/2026-11-10-chronicle-schema-design.md) for the spec and the decisions still open.

## The Spec

`Chronicle` is the spec as a C# library, with no dependencies. Reference it to read and write Chronicles;
apps in other languages use the JSON Schemas in [`schema/`](schema/), which are generated from it.

```csharp
using Chronicle;

var me = new Author { App = "scenario-builder", Actor = "designer@example.org", Via = Via.Person };
var record = ChronicleRecord.Start("harbor-ttx", "Harbor Ransomware Tabletop", EventKind.Exercise, DateTimeOffset.UtcNow);

// Publish: an entry adds, revises or retracts one item.
var entry = Entry.Add(record.Id, Part.World,
    new Fact { Id = "patient-zero", Topic = "groundTruth", Text = "The crew got in through a phishing email." },
    me, DateTimeOffset.UtcNow);
record = Projection.Append(record, entry);          // checks the rules, assigns the sequence

// Subscribe: each audience sees only what it may.
var participants = AudienceView.For(record, Audience.Participant);   // no ground truth
var json = ChronicleJson.Serialize(record);
var findings = Validation.Check(record);
```

| Type | What |
|---|---|
| `ChronicleRecord` | The record: header, five parts, history |
| `Entry` | One change, and what apps publish |
| `Item` and its kinds | What the parts hold: Cyber DEM objects, personas, objectives, planned events, activity, lessons |
| `Projection` | How entries make the parts |
| `AudienceView` | Who sees what |
| `Validation` | The rules the JSON Schema can't state |
| `Subscription`, `Topics` | What a subscriber asks for, and topic names for carriers that route by topic |
| `ChronicleJson`, `ChronicleSchema` | The JSON form, and the JSON Schema generated from the types |

## The API

`Chronicle.Api` stores Chronicles and carries their entries; it runs in Aspire as `chronicle-api` on
http://localhost:5285 (Swagger at `/swagger`). Entries are kept in SQLite (`chronicle.db`); the parts are rebuilt
from them on start. In Development it imports the samples in [`samples/`](samples/).

One controller per resource, in `Controllers/Api/`:

| Controller | Route | What |
|---|---|---|
| `ChroniclesController` | `GET /api/chronicles` | Every Chronicle |
| | `POST /api/chronicles` | Start one, or import one with its history (422 with findings if it breaks the rules) |
| | `GET /api/chronicles/{id}` | One Chronicle, as an audience sees it |
| | `GET /api/chronicles/{id}/findings` | The spec's validation findings |
| | `POST /api/chronicles/validate` | Check a record; stores nothing |
| `EntriesController` | `GET /api/chronicles/{id}/entries` | The history, filtered |
| | `GET /api/chronicles/{id}/entries/{sequence}` | One entry |
| | `POST /api/chronicles/{id}/entries` | Publish: 201 stored, 200 if already stored (safe to retry), 409 `STALE` or `ID_TAKEN`, 422 another rule |
| | `GET /api/chronicles/{id}/entries/stream` | Subscribe, as server-sent events: the history after `after`, then each new entry; resumes from `Last-Event-ID` |
| `LiveController` | `GET /api/chronicles/{id}/live` | **WebSocket**: the live feed for an audience (see below) |
| | `POST /api/chronicles/{id}/activity` | Report activity; with an `entry` it is notable and the entry is stored first |
| | `GET /api/chronicles/{id}/activity` | Recent activity an audience may see (`limit`, default 200) |
| | `GET /api/chronicles/{id}/subscribers` | The subscribers an audience may see |
| | `PUT /api/chronicles/{id}/subscribers/{subscriberId}` | Register or replace a subscriber, optionally with an agent |
| | `DELETE /api/chronicles/{id}/subscribers/{subscriberId}` | Remove one |
| | `POST /api/chronicles/{id}/subscribers/{subscriberId}/run` | Run its agent in the background: 202 `{runId, subscriberId}`; 409 `NO_AGENT` |
| | `GET /api/chronicles/{id}/context` | A view as text a model can read, with `items`, `total`, `withheld`, `tokens`, `sample` |
| `SchemaController` | `GET /api/schema/record`, `GET /api/schema/entry` | The JSON Schemas |

Entries are never changed or removed, so they take no PUT or DELETE. `Services/` holds the store, the sample seeder
and the handler that turns a broken spec rule into a problem response; `Models/` holds the query and summary types.

Reads take `audience` (participant, excon or designer; absent means participant) and `viewers` (persona, pool or
organization ids, matched against `knownBy`); entry reads also take `parts`, `kinds` and `after`. Repeat a parameter
for several values: `?parts=world&parts=intent`. **There is no authentication yet:** the audience is taken on trust,
so don't expose the API beyond a development range.

### The Live Side: Activity, Subscribers and Agents

**Activity** is what an app did as it happened: a GHOSTS NPC browsing, an email sent, an agent at work. It is the live
feed, not the record. The API keeps the last 1,000 per Chronicle **in memory** (a restart clears them). An activity
with an `entry` is **notable**: the entry is stored first, exactly as `POST /entries` would, and the activity carries
its `sequence`. So GHOSTS reports everything, and only what matters becomes history.

```json
POST /api/chronicles/salt-lantern/activity
{ "source": "ghosts", "actor": "persona-dana-whitlock", "kind": "email", "host": "saltlan-eng-ws01",
  "text": "Mailed IT on-call about the setpoint", "visibility": "participant", "data": { },
  "entry": { /* optional: an Entry; makes it notable */ } }
```

`source` and `kind` are required (kinds in use: `browse`, `email`, `file`, `chat`, `login`, `inject`, `agent.started`,
`agent.published`, `agent.failed`). `visibility` defaults to `excon`, so an unmarked report fails closed. For a
notable one it defaults to the entry's item's visibility. A reader also sees activity whose `actor` is one of its
`viewers`.

**Subscribers** are the apps and agents that read the Chronicle through their own view: `{app, name, role, audience,
viewers, parts, kinds, model, agent?}`. They are kept in SQLite (table `subscribers`), so they survive a restart. Their
activity counts and last activity are kept in memory; what they publish is written back. An audience sees subscribers
reading at its level or below, and only exercise control and designers see an agent's prompts (others get `hasAgent`).
Activity that names no `subscriberId` is counted against the reporting app's one agentless subscriber, so GHOSTS can
report without naming itself.

An **agent** is `{system, task, maxTokens?, output: {part, item, author, reason?}}`.

- `task` may hold `{{context}}`, which is replaced by the subscriber's view as text (`GET /context`). Without it, the context is appended.
- `output.item` is an item template whose strings may hold `{{text}}`, `{{title}}` (the first line), `{{body}}` (the rest), `{{now}}` and `{{runId}}`.

A run proceeds like this:

1. It emits `agent.started` (with `items`, `total`, `withheld`, `tokens`, `query`, `sample`).
2. It streams the model's words as `token` messages.
3. It strips markdown from the output.
4. It publishes the output: an add, or a revise if the item is already in the record.
5. It emits `agent.published` (with its `sequence`), or `agent.failed`.

Activity and tokens carry the subscriber's own audience as their visibility. The model is set under `Agents` in
`appsettings.json`:

- `Provider`: `ollama` (native `/api/chat` with `think: false`; Gemma's thinking otherwise eats the budget) or `openai` (any OpenAI-compatible `/v1/chat/completions`).
- `BaseUrl`, `Model`, `ApiKey` and `MaxTokens`. A subscriber's `model` overrides `Model`.

**The WebSocket** `GET /api/chronicles/{id}/live?audience=…&viewers=…` sends JSON text messages, each only if the
audience may see it:

| `type` | When | Body |
|---|---|---|
| `hello` | On open | `chronicleId`, `audience`, `subscribers`, `activity` (recent, oldest first) |
| `entry` | Each new entry | `entry` (filtered as the SSE stream is) |
| `activity` | Each new activity | `activity` |
| `subscriber` | A subscriber registered, changed or published | `subscriber` |
| `token` | While an agent writes | `subscriberId`, `runId`, `text` |

## The UI

`ui/` is a small Angular app named Chronicle: a short explanation, then a grid of events (name, kind, entries, created). Each event opens a page with its five parts, viewable as participant, exercise control or designer, and its latest entries, updated live. Its **Subscribers** page (`/events/{id}/subscribers`) shows each subscriber's view and agent output as it streams, plus the live activity feed, over the WebSocket. The page watches as exercise control by default. It runs in Aspire as
`chronicle-ui` on http://localhost:4320 and proxies `/api` (WebSockets included: `"ws": true` in `proxy.conf.json`) to the API on :5285. By hand: `cd ui && npm start`.
`npm install` can fail with `ENOTDIR` on the devcontainer's mount; install in `/tmp` and copy `node_modules` back, as the
[Scenario Builder README](../scenario-builder/README.md) shows.

### Mock Chronicles from the Scenario Builder

`tools/mock-from-scenario-builder.py` turns each scenario in the Scenario Builder (:5280) into a Chronicle: its
answers become World and Intent items, a GraySpace deployment becomes As Built, and each item gets one entry carrying
the decision behind it (time, person or agent, proposed or stated, reason). Files go to `samples/scenario-builder/`,
which the seeder doesn't read; `--post` imports them into the API (:5285). The mapping is a first guess, not the
Scenario Builder's publisher.

```bash
python3 src/chronicle/tools/mock-from-scenario-builder.py --post
```

## Working on It

```bash
dotnet test src/chronicle/Chronicle.Tests
dotnet test src/chronicle/Chronicle.Api.Tests
# After changing a type, regenerate the schemas and the sample, then review the diff:
CHRONICLE_UPDATE=1 dotnet test src/chronicle/Chronicle.Tests
dotnet test src/chronicle/Chronicle.Api.Tests
```

The tests fail when `schema/` or `samples/` is out of date.
