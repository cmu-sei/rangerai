# Scenario Builder

???+ info "Experimental"
    Scenario Builder is under active development, and its screens and data change often. The design documents are in [`src/scenario-builder/docs`](https://github.com/cmu-sei/rangerai/tree/master/src/scenario-builder/docs).

Scenario Builder walks someone who isn't an engineer through the questions that lead to a very good scenario. It keeps every decision and artifact consistent, and it works on a local model.

It's a structured workspace with an AI agent in a side panel. Nobody starts at an empty prompt: the **Questions** page is the spine, and the agent asks one question at a time, with a recommended answer first.

## Key Ideas

| Idea | Meaning |
|---|---|
| **Scenario type** | A kind of scenario, such as a Crucible tabletop or a small unit team exercise. It defines the stages, questions, gates, checks and outputs. Admins manage scenario types in **Admin › Scenario Types**, and a model can draft one from a few sentences. |
| **Stage** | A group of questions, answered before the next group opens. |
| **Gate** | What must be true before the next stage opens, for example "the owner approved the executive summary". |
| **Ledger** | The record of every question: open, answered or delegated, with who decided each answer, when and why. |
| **Stated vs. proposed** | A stated answer came from a person. A proposed answer came from the agent, after "you decide" or by inference, and is shown as such. |
| **Proposal** | A suggested change to a stated answer, shown as a card. The scenario's owner applies or discards it. |

The model chooses the wording and the order of the questions, but nothing counts as answered until a tool records it. The agent can edit delegated items and wording directly, and every edit is logged and can be reverted. Changing anything a person stated takes a proposal.

## The Workspace

Each scenario has these pages:

- **Context**: source documents and background for the scenario.
- **Questions**: every question by stage, with the agent panel alongside.
- **Summary**: the scenario as it stands.
- **Topology** (for scenario types with a range): the network and hosts, and where to deploy them.
- **Decision log**: every answer and edit, with who made it and why.
- **Finish**: the scenario JSON, documents rendered from templates, and "send to" buttons.

## Output

A scenario is one document, the **full scenario JSON**, described by [`schema/exercise-document.v0.schema.json`](https://github.com/cmu-sei/rangerai/blob/master/src/scenario-builder/schema/exercise-document.v0.schema.json). Other tools pull it from `GET /api/scenarios/{id}`. Everything else is rendered from it:

- **Documents people read** (read-ins, facilitator packs, designer reviews) come from **template libraries**: HTML or Markdown with Liquid placeholders, managed in **Admin › Libraries**. Each template declares its audience, and the builder removes everything that audience may not see before filling it in.
- **Send to** buttons for Blueprint, CITE, Gallery, Player, Steamfitter and GHOSTS are in place but answer "not connected yet".

Documents can be made at any stage. While a scenario has open findings, each document carries a draft banner.

## Topology and Deploy

A scenario type with a range has a Topology page. **Deploy to GraySpace** connects to a GraySpace, shows what it offers (its base, attachment points, images and roles), and builds and deploys the scenario's sites, segments and hosts. An Updraft screen shows a read-only preview of what Updraft offers, but isn't connected yet. Crucible is a stub. This is where [Range Automation](../range-automation.md) begins.

## Models

The API is the only path to a model, so keys stay on the server. Models are configured under `ModelServers` in the API's `appsettings.json`:

- **gemma4:26b on Ollama**: the target local model. Give it at least a 32k context, or it silently drops the start of the conversation.
- **A frontier model through LiteLLM**, such as Sonnet on Bedrock.
- **fake**: a rule-driven stand-in for plumbing checks.

Whether gemma-4-26b can guide a designer as well as a frontier model is measured, not assumed: an evaluation runs a scripted designer against both models and scores each run on structural checks.

## Run It

See [Local Development](../development.md), or the [Scenario Builder README](https://github.com/cmu-sei/rangerai/blob/master/src/scenario-builder/README.md) for running the API, UI and tests by hand.
