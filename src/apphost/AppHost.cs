// Orchestrates RangerAI's projects for local development: F5 in VS Code (or `dotnet run` here)
// starts every resource below and opens the Aspire dashboard with their logs and status.
// To add a project: reference it in RangerAI.AppHost.csproj (for .NET) and add it here.

var builder = DistributedApplication.CreateBuilder(args);

// ---- Chronicle (src/chronicle) ----
// The Chronicle API: apps publish entries and subscribe to them. Its "http" launch profile
// puts it on :5285; in Development it imports the sample Chronicles in src/chronicle/samples.
var chronicleApi = builder.AddProject<Projects.Chronicle_Api>("chronicle-api", launchProfileName: "http")
    .WithHttpHealthCheck("/health")
    .WithUrl("/swagger", "Swagger UI");

// ---- Scenario Builder (src/scenario-builder) ----
// Ports stay the ones in its README, since the UI's proxy.conf.json and the API's
// appsettings.json hard-code them: API :5280, UI :4310, fake model :5299.

// .NET API. Runs its "http" launch profile, so Aspire fronts it on :5280.
var scenarioApi = builder.AddProject<Projects.ScenarioBuilder_Api>("scenario-builder-api", launchProfileName: "http")
    .WithHttpHealthCheck("/api/scenario-types");

// Angular UI via `npm start` (its prestart builds the agent's browser bundle). ng serve
// binds only [::1] by default; VS Code's port forward connects over IPv4, hence --host.
// node_modules isn't installed here, because `npm install` fails with ENOTDIR on this
// devcontainer's mount; install it as the scenario builder README describes.
builder.AddExecutable("scenario-builder-ui", "npm", "../scenario-builder/ui",
        "start", "--", "--host", "0.0.0.0")
    .WithEnvironment("NG_CLI_ANALYTICS", "false")
    .WithEnvironment("NG_FORCE_AUTOCOMPLETE", "false")
    .WithHttpEndpoint(port: 4310, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithReference(scenarioApi)
    .WaitFor(scenarioApi);

builder.Build().Run();
