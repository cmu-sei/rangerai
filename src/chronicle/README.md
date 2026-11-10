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
| `SchemaController` | `GET /api/schema/record`, `GET /api/schema/entry` | The JSON Schemas |

Entries are never changed or removed, so they take no PUT or DELETE. `Services/` holds the store, the sample seeder
and the handler that turns a broken spec rule into a problem response; `Models/` holds the query and summary types.

Reads take `audience` (participant, excon or designer; absent means participant) and `viewers` (persona, pool or
organization ids, matched against `knownBy`); entry reads also take `parts`, `kinds` and `after`. Repeat a parameter
for several values: `?parts=world&parts=intent`. **There is no authentication yet:** the audience is taken on trust,
so don't expose the API beyond a development range.

## Working on It

```bash
dotnet test src/chronicle/Chronicle.Tests
dotnet test src/chronicle/Chronicle.Api.Tests
# After changing a type, regenerate the schemas and the sample, then review the diff:
CHRONICLE_UPDATE=1 dotnet test src/chronicle/Chronicle.Tests
dotnet test src/chronicle/Chronicle.Api.Tests
```

The tests fail when `schema/` or `samples/` is out of date.
