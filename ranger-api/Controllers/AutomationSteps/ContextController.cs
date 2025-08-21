using Microsoft.AspNetCore.Mvc;

namespace RangerAi.Controllers.AutomationSteps;

public class ContextRequest
{
    public Actor Actor { get; set; }
    public string Trigger { get; set; }
}

[ApiController]
[Route("api/automation/context")]
public class ContextController : Controller
{
    private readonly IContextService _service;

    public ContextController(IContextService service)
    {
        _service = service;
    }

    [HttpPost("build")]
    public async Task<ActionResult<ContextFrame>> Build([FromBody] ContextRequest req)
    {
        var context = await _service.BuildAsync(req.Actor, req.Trigger);
        return Ok(context);
    }
}

public interface IContextService
{
    Task<ContextFrame> BuildAsync(Actor actor, string trigger);
}

public class SimpleContextService : IContextService
{
    public Task<ContextFrame> BuildAsync(Actor actor, string trigger)
    {
        var frame = new ContextFrame
        {
            Actor = actor,
            Trigger = trigger,
            StateSummary = $"Recent actions for {actor.Name} are not yet implemented."
        };
        return Task.FromResult(frame);
    }
}

public class ContextFrame
{
    public Actor Actor { get; set; }                      // The actor involved in this context
    public string Trigger { get; set; }                   // Event or message they’re reacting to
    public string StateSummary { get; set; }              // Optional: summary of recent activity, status, etc.
    public Dictionary<string, object> Attachments { get; set; } = new(); // Optional structured data
}
