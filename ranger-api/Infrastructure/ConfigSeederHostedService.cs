using System.Text.Json;

namespace RangerAi.Infrastructure;

/// <summary>
/// Defines a contract to seed configuration data (models and workflows) at startup.
/// </summary>
public interface IConfigSeeder
{
    /// <summary>
    /// Ensures configuration.json is populated. Fetches from Ollama and n8n if missing or empty.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the seeding operation.</param>
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public class OllamaN8nConfigSeeder : IConfigSeeder
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaN8nConfigSeeder> _logger;
        private const string FilePath = "config/configuration.json";

        public OllamaN8nConfigSeeder(HttpClient httpClient, ILogger<OllamaN8nConfigSeeder> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (File.Exists(FilePath))
            {
                var content = await File.ReadAllTextAsync(FilePath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(content) && JsonSerializer.Deserialize<JsonElement>(content).GetRawText().Length > 2)
                {
                    _logger.LogInformation("Configuration file exists and has entries; skipping seeding.");
                    return;
                }
            }

            _logger.LogInformation("Seeding configuration: fetching from Ollama and n8n...");

            // Fetch models
            JsonElement models;
            var url = $"{ApplicationDetails.GetOllamaHost()}/api/tags";

            try
            {
                var modelsResponse = await _httpClient.GetAsync(url, cancellationToken);
                modelsResponse.EnsureSuccessStatusCode();
                var modelsJson = await modelsResponse.Content.ReadAsStringAsync(cancellationToken);
                var modelsRoot = JsonSerializer.Deserialize<JsonElement>(modelsJson);
                models = modelsRoot.GetProperty("models");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch models from {url}.");
                throw;
            }


            // Fetch workflows
            var apiKey = ApplicationDetails.GetWorkflowApiKey() ?? string.Empty;
            url = $"{ApplicationDetails.GetWorkflowHost()}/api/v1/workflows?active=true";
            var workflowsRequest = new HttpRequestMessage(HttpMethod.Get, url);
            workflowsRequest.Headers.Add("Accept", "application/json");
            workflowsRequest.Headers.Add("X-N8N-API-KEY", apiKey);

            _logger.LogInformation($"Making request to {workflowsRequest.RequestUri} with key {apiKey}");

            JsonElement workflows;
            try
            {
                var workflowsResponse = await _httpClient.SendAsync(workflowsRequest, cancellationToken);
                workflowsResponse.EnsureSuccessStatusCode();
                var workflowsJson = await workflowsResponse.Content.ReadAsStringAsync(cancellationToken);
                var workflowsRoot = JsonSerializer.Deserialize<JsonElement>(workflowsJson);
                workflows = workflowsRoot.GetProperty("data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch workflows from {url}. Key length: {apiKey.Length}");
                throw;
            }

            // Combine
            var config = new
            {
                Models = models,
                Workflows = workflows
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var outJson = JsonSerializer.Serialize(config, options);

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            await File.WriteAllTextAsync(FilePath, outJson, cancellationToken);
            _logger.LogInformation("Configuration seeding completed: written to {FilePath}", FilePath);
        }
    }

/// <summary>
/// Hosted service that runs once at startup to ensure configuration.json is populated
/// with Ollama models and n8n workflows if missing or empty.
/// </summary>
public class ConfigSeederHostedService(
    IConfigSeeder configSeeder,
    ILogger<ConfigSeederHostedService> logger)
    : IHostedService
{
    /// <summary>
    /// Called by the host to start the service. Executes once at startup.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ConfigSeederHostedService: Checking configuration seeding...");

        try
        {
            await configSeeder.SeedAsync(cancellationToken);
            logger.LogInformation("ConfigSeederHostedService: Seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ConfigSeederHostedService: Error during seeding.");
            // Throw to prevent app from starting with invalid config
            throw;
        }
    }

    /// <summary>
    /// Called by the host to stop the service. No-op for seeding.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ConfigSeederHostedService: Stopping.");
        return Task.CompletedTask;
    }
}
