# Chronicle

???+ info "Draft"
    Chronicle has a draft spec and a running API. Nothing in the spec is settled until the architecture team agrees on it; the [schema design](https://github.com/cmu-sei/rangerai/blob/master/src/chronicle/docs/specs/2026-11-10-chronicle-schema-design.md) lists the decisions still open. The concept brief is [`src/chronicle/chronicle.html`](https://github.com/cmu-sei/rangerai/blob/master/src/chronicle/chronicle.html); open it in a browser or print it.

Chronicle is one living record of an event, from the first idea to the last lesson. The event can be training, an exercise or a simulation, run at a table or on a live range.

It starts as the scenario: what the world looks like, and what the event must achieve. Around it, apps publish and subscribe. Each one subscribes to the parts it needs and publishes what it built, did and saw. When the event ends, the Chronicle is the event: complete, attributable, and ready to seed the next one.

## At a Glance

| | |
|---|---|
| **What it is** | One JSON record per event, with a published, versioned schema |
| **How apps connect** | Publish and subscribe around the record. No app calls another. |
| **It starts as** | The scenario: the world, and what the event must achieve |
| **It ends as** | The event itself: built, run and reviewed |
| **Models** | Local or frontier. Works on a closed range. |

## Four Principles

1. **One record, many authors.** Each app owns its part. It reads the rest, but writes only what it did.
2. **Publish and subscribe.** Apps subscribe to the parts they need and publish what they add. A new app is a new subscriber, not a new integration.
3. **Nothing is overwritten.** Every change keeps who made it, when and why, so the record is also the event's history.
4. **The right eyes only.** Ground truth stays with exercise control. Participants see only what they should, everywhere.

## The Record

A Chronicle has five parts, filled in the order the event fills them:

| Part | What it holds | Written by |
|---|---|---|
| **World** | Sides, organizations, networks, devices, services, personas and pools, vulnerabilities, facts | Scenario Builder |
| **Intent** | Objectives, teaching points, rules of play, vocabulary, summaries, the planned events | Scenario Builder |
| **As Built** | Deployments, what each planned item became, generated personas and content | GraySpace, GHOSTS, Pandora |
| **As Run** | Milestones, cyber and influence activity, injects delivered, participant actions, observations | GHOSTS, the exercise apps, exercise control |
| **Lessons** | Assessments against the objectives, lessons for the next event | After-action review |

Apps don't edit the record. They publish **entries**: each one adds, revises or retracts one item, and records who made the change, when and why. The parts are rebuilt from the entries, so the record is also its own history. A revise names the version it replaces, so two apps can't silently overwrite each other.

Every item is marked **participant**, **excon** (exercise control) or **designer**. An item nobody marked stays with exercise control, so a forgotten marking can't leak the ground truth. An item can also name the personas, pools or organizations that know it: a simulated bank clerk knows their own bank, but not the twist.

### Built on the Standards

The spec borrows its names from SISO's standards, so a Chronicle can be exchanged with simulations that already use them:

- **Cyber objects and events** use the names of the Cyber Data Exchange Model (SISO-STD-025-2023): a host is a `Device`, an attack is a `PhishingAttack`. Enumerated values are written as SISO-REF-010 writes them, such as `MicrosoftWindows`. ATT&CK ids are supporting detail.
- **Sides** follow MSDL's and C2SIM's force sides.
- **Personas** meet the needs the GHOSTS team set out for the Cyber DEM: an account for each service, a pool form, where a persona came from, and no credentials on the persona.
- **What the standards don't model** is Ranger's own: vulnerabilities, injects for people, objectives, visibility, and who said what.

### The Spec

The spec is the C# library `Chronicle`, in [`src/chronicle`](https://github.com/cmu-sei/rangerai/tree/master/src/chronicle). Apps in other languages use its JSON Schemas, which are generated from it so the two can't drift:

| File | What |
|---|---|
| [`schema/chronicle-record.v0.schema.json`](https://github.com/cmu-sei/rangerai/blob/master/src/chronicle/schema/chronicle-record.v0.schema.json) | A whole record |
| [`schema/chronicle-entry.v0.schema.json`](https://github.com/cmu-sei/rangerai/blob/master/src/chronicle/schema/chronicle-entry.v0.schema.json) | One entry, as apps publish it |
| [`samples/harbor-ttx.chronicle.json`](https://github.com/cmu-sei/rangerai/blob/master/src/chronicle/samples/harbor-ttx.chronicle.json) | A worked example: a fictional tabletop exercise through all four stages |

## The API

The Chronicle API stores Chronicles and carries their entries. Locally it runs in Aspire as `chronicle-api` at <http://localhost:5285>, with Swagger at `/swagger`, and loads the sample exercise. See [Local Development](development.md).

| Resource | Routes |
|---|---|
| Chronicles | `GET /api/chronicles`, `POST /api/chronicles`, `GET /api/chronicles/{id}`, `GET /api/chronicles/{id}/findings`, `POST /api/chronicles/validate` |
| Entries | `GET /api/chronicles/{id}/entries`, `GET /api/chronicles/{id}/entries/{sequence}`, `POST /api/chronicles/{id}/entries` |
| Subscription | `GET /api/chronicles/{id}/entries/stream`: server-sent events, the history first and then each new entry |
| Live | `GET /api/chronicles/{id}/live`: a WebSocket with the subscribers, activity, new entries and agents' words as they're written |
| Activity | `POST /api/chronicles/{id}/activity` (with an `entry`, it's notable and stored), `GET /api/chronicles/{id}/activity` |
| Subscribers | `GET /api/chronicles/{id}/subscribers`, `PUT`/`DELETE /api/chronicles/{id}/subscribers/{subscriberId}`, `POST …/{subscriberId}/run` |
| Context | `GET /api/chronicles/{id}/context`: a view as text a model can read, with what it withholds |
| Schema | `GET /api/schema/record`, `GET /api/schema/entry` |

Activity is the live feed, kept in memory (the last 1,000 per Chronicle). Only notable activity, the kind that carries an entry, becomes part of the record. Subscribers are stored with the Chronicle; one registered with an agent can be run, and its output is published back. The Chronicle UI shows all of it, live, on each event's **Subscribers** page.

Every read is an audience's view: `?audience=participant` (the default), `excon` or `designer`, and `?viewers=` for the personas or pools a simulated user speaks for. Entry reads and subscriptions also filter by `parts`, `kinds` and `after`.

???+ warning "No authentication yet"
    The API takes the audience a caller asks for on trust. Don't expose it beyond a development range.

## Why It Matters for AI

A language model doesn't know your exercise. Give it the Chronicle and it stops guessing. Because the record is structured, each AI gets exactly the parts its job needs, not a pile of documents. That matters most on a closed range, where a smaller local model has room for less context.

| AI at work | Reads from the Chronicle | Never sees |
|---|---|---|
| Scenario agent (Scenario Builder) | The designer's sources and the scenario so far | Nothing held back: it works for the designer |
| Simulated users (GHOSTS) | Their persona and organization, their platforms, events as they happen | The ground truth and the plan |
| Content generation | The world, the information environment, the planned events | Designer-only notes |
| Exercise control assistant *(next)* | The intent, the ground truth, the escalation steps, what has happened | Nothing held back: it works for exercise control |
| After-action analyst *(next)* | Everything: intent, what was built, every inject, decision and action | Nothing held back: it reports to designers |

## Where It Stands

- **Today:** Scenario Builder writes the world and intent. GraySpace builds a range from them. GHOSTS runs the simulated users. The Chronicle spec is drafted, and the Chronicle API stores Chronicles, takes entries and streams them to subscribers.
- **Next:** agreeing the spec's open decisions, authentication for the API, and every app publishing what it built, did and saw: Scenario Builder first, then GHOSTS subscribing.
