using System.Text.Json.Nodes;

namespace RangerAi.Services;

public class ConfigurationService
{
    public async Task<WorkflowDetail> GetWorkflow(string workflowId)
    {
        var filePath = "config/configuration.json";
        if (File.Exists("config/configuration.Development.json"))
            filePath = "config/configuration.Development.json";

        var t = await File.ReadAllTextAsync(filePath);
        var j = JsonNode.Parse(t);

        var workflows = j?["Workflows"]?.AsArray();
        if (workflows != null)
        {
            foreach (var workflow in workflows)
            {
                var method = string.Empty;
                var path = string.Empty;
                var name = workflow?["name"]?.ToString();
                var id = workflow?["id"]?.ToString();
                Console.WriteLine($"{name} ({id})");

                foreach (var node in workflow?["nodes"]?.AsArray())
                {
                    if (node?["type"]?.ToString() == "n8n-nodes-base.webhook")
                    {
                        // get parameters/httpMethod parameters/path
                        method = node?["parameters"]["httpMethod"]?.ToString();
                        path = node?["parameters"]["path"]?.ToString();
                    }
                }

                if (name.Equals(workflowId.Replace("workflow:", ""), StringComparison.InvariantCultureIgnoreCase))
                {
                    return new WorkflowDetail
                    {
                        Id = id,
                        Name = name,
                        Method = method,
                        Path = path
                    };
                }
            }
        }

        return null;
    }

    public class WorkflowDetail
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
    }
}
