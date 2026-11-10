# The Chronicle Schema, Version 0.1

2026-11-10. Proposed; nothing here is settled until the architecture team agrees on it. Built from the
[Chronicle concept brief](../../chronicle.html), the OIEX standards mapping and the GHOSTS persona brief
(`reference/standards/`).

## What It Is

A Chronicle is one record of one event. The spec is the C# library `Chronicle`: plain records, no
dependencies. The JSON Schema in `schema/` is generated from it, so the two can't drift; a test fails when the
committed schema is out of date.

| File | What |
|---|---|
| `Chronicle/` | The spec: the types, the JSON form, the projection, audience and validation rules |
| `schema/chronicle-record.v0.schema.json` | A whole record |
| `schema/chronicle-entry.v0.schema.json` | One entry, as apps publish it |
| `samples/harbor-ttx.chronicle.json` | A worked example: a fictional tabletop, all four stages, with a revise and a retract |

## The Shape

```text
ChronicleRecord
  schemaVersion, id, name, kind (training | exercise | simulation), created, seededFrom
  world     [Item]   what the world looks like
  intent    [Item]   what the event must achieve, and how it is planned to unfold
  asBuilt   [Item]   what was built
  asRun     [Item]   what happened
  lessons   [Item]   what was learned
  history   [Entry]  every change, in order
```

**An entry is the unit apps publish.** It adds, revises or retracts one item in one part, and carries who
(`author`: app, actor, via person, agent or app), when (`recordedAt`), and why (`reason`). A store gives it the next
`sequence`. Entries are never changed or removed.

**The parts are a view of the history.** `Projection.Rebuild` replays the history into the parts; validation fails
a record whose parts don't match. Every reader and store applies the same rules:

- An add puts a new item at the end of its part. An id is never reused, even after a retract.
- A revise replaces the item in full. It names the entry that wrote the version it replaces (`revises`); if a later
  entry has replaced that version, the store refuses it (`STALE`). Two authors can't silently overwrite each other.
- A retract withdraws the item. It stays in the history.
- An item never changes part or kind.

**Every item has** a `kind`, an `id` unique across the whole Chronicle, a `visibility` and a `standing` (stated by a
person, or proposed by an agent or app). Each kind belongs to fixed parts; a `fact` can't be filed under Lessons.

## The Items

| Part | Kinds | From |
|---|---|---|
| World | `side` | MSDL's and C2SIM's force sides |
| | `organization` | Ranger (GHOSTS needs an employer for every persona) |
| | `network`, `networkLink`, `device`, `operatingSystem`, `service`, `application`, `system`, `data`, `persona` | Cyber DEM objects (SISO-STD-025.1-2023), Cyber DEM names |
| | `personaPool` | The GHOSTS persona brief: "40 office staff on this segment" |
| | `vulnerability`, `defense` | Ranger: the Cyber DEM has neither as an object |
| | `fact` | Ranger: background, situation, information environment; ground truth is a fact at excon |
| Intent | `objective`, `teachingPoint`, `rule`, `term` | Scenario Builder's exercise document |
| | `plannedEvent` | The master scenario events list. It carries an `inject` for people, a `cyber` event in Cyber DEM terms, an `influence` activity, or several |
| As Built | `deployment`, `realization` | What a builder made, and what each planned item became (vm id, hostname, address) |
| | cyber objects, `persona`, `content` | What builders add: a jump box, generated NPCs, Pandora's articles |
| As Run | `milestone`, `cyberActivity`, `influenceActivity`, `injectDelivery`, `participantAction`, `observation` | What happened, each with `at` and the `plannedEventId` it carried out |
| Lessons | `assessment`, `lesson` | Each objective's rating with evidence; what to do next time |
| Any | `extension` | An app's own data, or a kind proposed for a later version |

## How It Uses the Standards

The library follows the four steps the OIEX mapping proposes:

1. **Cyber DEM names.** Cyber objects keep the Cyber DEM's class and attribute names (`Device.deviceTypes`,
   `NetworkInterface.relatedObjectId`, `OperatingSystem.osType`). An item's id is the Cyber DEM `ObjectID`, so each
   world item maps to one Cyber DEM object without loss.
2. **SISO-REF-010 values as SISO writes them.** Enumerated fields are strings, such as `MicrosoftWindows`, and the
   schema cites each table (`"x-siso-ref-010": { "uid": 691, ... }`). Ranger's own enumerations are camelCase.
3. **Events name their Cyber DEM class first** (`PhishingAttack`, `Degrade`). ATT&CK ids are supporting detail
   (BONES pattern 6). `details` holds what no class models yet (pattern 4). `sourceIds` chains events (Figure 2).
4. **What the Cyber DEM doesn't model stays Ranger's:** vulnerabilities, defenses in place, injects for people,
   objectives, the ledger (author, standing, reason) and visibility.

The GHOSTS persona brief's nine needs:

| Need | In the schema |
|---|---|
| 1. An account for each service | `persona.accounts[]`: `serviceId`, `username`, `displayName` |
| 2. Organization and role | `persona.organizationId`, `persona.role` |
| 3. Devices and services used | `relatedObjects` with `Uses`, a value proposed to SISO and not yet in SISO-REF-010 |
| 4. A pool form | `personaPool`; a generated persona names its `poolId` |
| 5. Where a persona came from | `persona.origin`: authored, generated, modelledOnRealPerson |
| 6. What a persona may know | `knownBy` on any item: it answers the brief's question 3 with "on each piece of information" |
| 7. Sensitive traits | `persona.traits[]`, each with an optional SISO `sensitivity` |
| 8. No credentials | `account` has no password field; credentials are `data` of type `Credentials` |
| 9. Influence events | `influence` (post, comment, react, share, message), planned or as run |

## Who Sees What

Visibility is an ordered ladder: `participant`, `excon` and `designer`, the same three rungs Scenario Builder uses.
An audience sees what is marked at or below its rung, plus any item whose `knownBy` names one of its viewers. A
simulated bank clerk subscribes as `participant`, with viewers set to its persona, pool and organization; it sees
what participants see and what its bank knows, and never the ground truth.

**An item nobody marked is excon.** It fails closed: forgetting a marking hides an item from participants, and
can't leak the twist to them. Scenario Builder's own items default to participant; whatever writes its items into a
Chronicle should set every visibility explicitly.

`AudienceView.For` applies the rule to a record. A carrier applies the same rule to each entry, using the version
the entry writes, or for a retract the version it withdraws.

## Publish and Subscribe

The spec fixes the messages, not the transport. An app publishes an `Entry`; a `Subscription` asks for a Chronicle,
parts, kinds, an audience and viewers, from a sequence number. Carriers that route by topic use
`chronicle.{chronicleId}.{part}.{kind}`, such as `chronicle.harbor-ttx.world.device`. `kind` is on the entry
itself, so a retraction routes like the item it withdraws.

The Chronicle API (`Chronicle.Api`) is the first carrier: `POST /api/chronicles/{id}/entries` publishes, and
`GET /api/chronicles/{id}/entries/stream` subscribes as server-sent events, with the subscription's fields as query
parameters. See the [README](../../README.md#the-api).

## Versioning

`schemaVersion` is semantic: adding an optional field or a kind is a minor version; renaming a field or changing
its meaning is a major version. The schema's `$id` carries the major version (`urn:ranger:chronicle:record:v0`).
Readers refuse an unknown kind, an unknown property and an integer where an enumeration string belongs; a kind not
yet in the schema travels as an `extension`.

## Decisions to Make

1. **Is the history the record?** This draft says yes: the parts are a view, and a record without its history
   can't be checked. The alternative is a document with a separate audit log, which is simpler to edit by hand and
   loses the "nothing is overwritten" guarantee.
2. **Revise replaces the item in full.** It is simple to check and to read. A patch form would make entries
   smaller, and would make concurrency checks per field instead of per item.
3. **Visibility fails closed to excon.** Scenario Builder defaults to participant. One of the two should change so
   that the same unmarked item means the same thing everywhere.
4. **`knownBy` per item, or a per-persona knowledge list?** Per item travels with the data, and is what this draft
   does. A per-persona list is easier to review for one persona.
5. **SISO-REF-010 values aren't checked yet.** The reference folder holds Git LFS pointers, not the files; with
   `SISO-REF-010.xml` in hand, a validator can check each field against the table its schema cites. The constants
   in `CyberDem.cs` are only the values the reference material quotes.
6. **Durations are .NET's `[d.]hh:mm:ss` form** (`01:40:00`), which is easy for any language to parse.
   ISO 8601 durations (`PT1H40M`) are the other choice, and closer to C2SIM.
7. **The header (name, description) has no history.** A rename could become an entry against the record itself.
8. **A C2SIM form.** The mapping's decision 6 still stands. This draft is the JSON form; an OWL extension, if OIEX
   becomes one, would map to it.
