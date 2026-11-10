# RangerAI.AppHost

The [Aspire](https://aspire.dev) AppHost for local development. Press **F5** in VS Code
("Aspire: RangerAI (all services)") or run `aspire run` from the repo root to start everything,
then watch logs and status in the Aspire dashboard (http://localhost:15180).

| Resource | URL | What |
|---|---|---|
| `chronicle-api` | http://localhost:5285 | Chronicle API: publish and subscribe to Chronicles (Swagger at `/swagger`) |
| `scenario-builder-api` | http://localhost:5280 | Scenario Builder .NET API |
| `scenario-builder-ui` | http://localhost:4310 | Scenario Builder Angular UI (proxies `/api` to :5280) |
| `scenario-builder-fake-model` | http://localhost:5299 | Stand-in model server (the API's `fake` model) |

The UI expects `node_modules` already installed in `scenario-builder/ui` and `scenario-builder/agent`
(see the scenario builder README). Ollama and LiteLLM are still started separately.

## Adding a project

- **.NET:** add a `<ProjectReference>` in `RangerAI.AppHost.csproj`, then
  `builder.AddProject<Projects.Your_Project>("name")` in `AppHost.cs`.
- **Node/npm:** `builder.AddJavaScriptApp("name", "../path", "script")` or `AddNodeApp(...)`.
- **Python:** add the `Aspire.Hosting.Python` package and use `AddPythonApp` / `AddUvicornApp`.

Keep the `Aspire.AppHost.Sdk`/`Aspire.Hosting.*` versions in step with `.devcontainer/features/aspire`.
