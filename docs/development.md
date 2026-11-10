# Local Development

The repository includes a devcontainer and an [Aspire](https://aspire.dev) AppHost that starts every service together.

## Start Everything

1. Open the repository in its devcontainer.
2. Install the Scenario Builder's `node_modules` in `src/scenario-builder/ui` and `src/scenario-builder/agent`. On the devcontainer's mount, `npm install` can fail with `ENOTDIR`; the [Scenario Builder README](https://github.com/cmu-sei/rangerai/blob/master/src/scenario-builder/README.md) shows the workaround.
3. Press **F5** in VS Code ("Aspire: RangerAI (all services)"), or run `aspire run` from the repository root.
4. Watch logs and status in the Aspire dashboard at <http://localhost:15180>.

| Service | URL | What |
|---|---|---|
| `chronicle-api` | <http://localhost:5285> | Chronicle API: publish and subscribe to Chronicles (Swagger at `/swagger`) |
| `scenario-builder-api` | <http://localhost:5280> | Scenario Builder .NET API |
| `scenario-builder-ui` | <http://localhost:4310> | Scenario Builder Angular UI (proxies `/api` to :5280) |

Ollama, LiteLLM and the stand-in `fake` model server (:5299) are started separately. See [Scenario Builder › Models](scenario-builder/index.md#models).

## Tests

```bash
dotnet test src/RangerAI.slnx
```

The Chronicle tests also check that the generated JSON Schemas and the sample in `src/chronicle` are current. After
changing a Chronicle type, regenerate them with `CHRONICLE_UPDATE=1 dotnet test src/chronicle/Chronicle.Tests` and
review the diff.

## Adding a Project

New parts are added to the AppHost in `src/apphost`. See its [README](https://github.com/cmu-sei/rangerai/blob/master/src/apphost/README.md) for .NET, Node and Python projects.

## These Docs

The site is built with [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/). Run `mkdocs serve` from the repository root to preview it at <http://localhost:8000>. See [contributing.md](https://github.com/cmu-sei/rangerai/blob/master/contributing.md).
