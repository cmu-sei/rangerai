using Microsoft.AspNetCore.Mvc;

namespace RangerAi.Controllers.AutomationSteps;

[ApiController]
[Route("api/automation/entity")]
public class EntityController : Controller
{
    private readonly IEntityService _service;

    public EntityController(IEntityService service)
    {
        _service = service;
    }

    [HttpPost("resolve")]
    public ActionResult<IEnumerable<Actor>> Resolve([FromBody] List<string> names)
    {
        return Ok(_service.Resolve(names));
    }
}


public interface IEntityService
{
    IEnumerable<Actor> Resolve(List<string> names);
}

public class TagEntityService : IEntityService
{
    private readonly IEnumerable<Actor> _actors;

    public TagEntityService(IEnumerable<Actor> actors)
    {
        _actors = actors;
    }

    public IEnumerable<Actor> Resolve(List<string> names)
    {
        return _actors.Where(a => names.Any(n => a.Name.Contains(n, StringComparison.OrdinalIgnoreCase)));
    }
}

public class Actor
{
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; }                          // e.g., "person", "system", "sensor", "document"
    public string Role { get; set; }                          // e.g., "SOC analyst", "router", "controller"
    public string Domain { get; set; }                        // e.g., "cyber", "logistics", "comms"

    public bool IsInitiator { get; set; }                     // Can issue or generate actions
    public bool IsTargetable { get; set; }                    // Can be acted upon

    public Dictionary<string, string> Metadata { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();

    public List<string> Tags { get; set; } = new();           // Classification: "ops", "external", "VIP", etc.
}
