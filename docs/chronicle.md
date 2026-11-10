# Chronicle

???+ info "Concept"
    Chronicle is a concept under design. The full brief is [`src/chronicle/chronicle.html`](https://github.com/cmu-sei/rangerai/blob/master/src/chronicle/chronicle.html); open it in a browser or print it.

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

- **Today:** Scenario Builder writes the world and intent. GraySpace builds a range from them. GHOSTS runs the simulated users.
- **Next:** subscriptions, and every app writing back what it built, did and saw.
