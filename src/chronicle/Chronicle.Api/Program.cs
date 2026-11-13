// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Chronicle;
using Chronicle.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Bodies read and write exactly as the spec's own JSON does.
builder.Services.AddControllers()
    .AddJsonOptions(options => ChronicleJson.Apply(options.JsonSerializerOptions));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ChronicleExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddSingleton(sp => new ChronicleStore(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Chronicle") ?? "Data Source=chronicle.db",
    sp.GetRequiredService<ILogger<ChronicleStore>>()));

// The live side: activity, subscribers (in the same SQLite), their agents and the WebSocket that carries it all.
builder.Services.AddSingleton<LiveHub>();
builder.Services.AddSingleton(sp => new SubscriberStore(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Chronicle") ?? "Data Source=chronicle.db"));
builder.Services.AddSingleton<ActivityService>();
builder.Services.AddSingleton<AgentRunner>();
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agents"));
builder.Services.AddHttpClient(AgentRunner.Client, c => c.Timeout = TimeSpan.FromMinutes(5));

// OpenAPI document at /openapi/v1.json, browsable at /swagger (Development only).
builder.Services.AddOpenApi();

var app = builder.Build();

var store = app.Services.GetRequiredService<ChronicleStore>();
store.Load();
app.Services.GetRequiredService<SubscriberStore>().Load();
if (app.Configuration.GetValue<bool>("Samples:Seed"))
    await SampleSeeder.Seed(store,
        Path.GetFullPath(app.Configuration["Samples:Directory"] ?? "../samples", app.Environment.ContentRootPath), app.Logger);

app.UseExceptionHandler();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Chronicle API v1");
        options.DocumentTitle = "Chronicle API";
    });
}

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

public partial class Program;
