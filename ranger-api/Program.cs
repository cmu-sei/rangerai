// Copyright 2025 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using NLog;
using RangerAi.Infrastructure;
using RangerAi.Infrastructure.Filters;
using RangerAi.Infrastructure.Transformers;

var _log = LogManager.GetCurrentClassLogger();
_log.Info($"Ranger Initializing...");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc($"v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "RANGER API",
        Description = $"RANGER API v1 - Assembly: {ApplicationDetails.Version}",
        Contact = new OpenApiContact
        {
            Name = "Ranger Team",
            Email = "info [*at*] sei.cmu.edu",
            Url = new Uri("https://sei.cmu.edu")
        },
        License = new OpenApiLicense
        {
            Name = "Copyright 2025 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(new LowercaseParameterTransformer()));
    options.Filters.Add(new ProducesAttribute("application/json"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR(options =>
{
    options.AddFilter<LoggingHubFilter>();
});
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IConfigSeeder, OllamaN8nConfigSeeder>();
builder.Services.AddHostedService<ConfigSeederHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"RANGER API v1");
});
app.UseSession();

app.UseMiddleware<RequestLoggingFilter>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
#pragma warning disable ASP0014
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Public}/{action=Index}/{id?}");
});
#pragma warning restore ASP0014

_log.Info($"Ranger Starting Up...");

app.Run();
