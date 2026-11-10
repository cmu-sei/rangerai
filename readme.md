# Ranger

Ranger is AI tooling for cyber range training, exercises and simulation. It runs on local or frontier models, so it works on closed and air-gapped ranges, and it works alongside the SEI's [Crucible Framework](https://cmu-sei.github.io/crucible/) and [GHOSTS](https://github.com/cmu-sei/GHOSTS).

![Ranger Logo](docs/assets/ranger-face-sm.png)

> **Ranger is being reorganized.** The earlier Ranger stack (the chat API, workflows, MCP and appliance under `src/ranger`) is being retired. Ranger is now built from the parts below.

## The Parts

| Part | Status | What it does |
|---|---|---|
| [Scenario Builder](src/scenario-builder/) | Experimental | Walks a designer who isn't an engineer through the questions that lead to a good scenario, with an AI agent in a side panel. Its output is the scenario JSON that starts a Chronicle. |
| [Chronicle](src/chronicle/) | Draft | One living record of an event, from the first idea to the last lesson. Apps publish to it and subscribe to it, and don't have to call each other for data. We feel this is important as AI applications need context to make the right decisions. The draft spec is a C# library with generated JSON Schemas, and the Chronicle API stores Chronicles and carries their entries. |
| Range Automation | Next | Builds, runs and adapts the range from the Chronicle: deploying environments, driving GHOSTS NPCs, and triggering injects during play. |

## Why

An exercise today lives in pieces: a scenario document, a network build, injects in a spreadsheet, activity in logs, and lessons in someone's notes. Each piece takes an expert, and AI models given only one piece invent the rest. Ranger keeps the whole event in one structured record, so people and AI models get the context their job needs. Visibility rules keep the ground truth away from participants.

## Getting Started

- **Documentation:** <https://cmu-sei.github.io/rangerai/>
- **Run Scenario Builder:** see [src/scenario-builder/README.md](src/scenario-builder/README.md).
- **Run everything locally:** press **F5** in VS Code, or run `aspire run` from the repository root. See [src/apphost/README.md](src/apphost/README.md).
- **Read the Chronicle concept brief:** open [src/chronicle/chronicle.html](src/chronicle/chronicle.html) in a browser.
- **Read the Chronicle spec:** see [src/chronicle/README.md](src/chronicle/README.md) and the [schema design](src/chronicle/docs/specs/2026-11-10-chronicle-schema-design.md).

## Repository Layout

| Folder | What |
|---|---|
| `src/scenario-builder/` | Scenario Builder: .NET API, Angular UI, TypeScript agent, scenario types and schema |
| `src/chronicle/` | Chronicle: the concept brief, the draft spec (the `Chronicle` library, its JSON Schemas and a sample), and the Chronicle API |
| `src/apphost/` | Aspire AppHost for local development |
| `src/ranger/` | The earlier Ranger stack, being retired |
| `docs/` | This documentation site (MkDocs) |

## Contributing

See [contributing.md](contributing.md).

## License

Copyright 2022 Carnegie Mellon University. See the [license.md](license.md) file for details.
