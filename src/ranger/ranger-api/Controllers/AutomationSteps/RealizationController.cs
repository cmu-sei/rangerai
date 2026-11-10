using Microsoft.AspNetCore.Mvc;

namespace RangerAi.Controllers.AutomationSteps;

public class RealizationRequest
{
    public Actor Actor { get; set; }
    public ActionIntent Intent { get; set; }
}

[ApiController]
[Route("api/automation/realization")]
public class RealizationController : Controller
{
    private readonly IRealizationService _service;

    public RealizationController(IRealizationService service)
    {
        _service = service;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] RealizationRequest req)
    {
        await _service.RealizeAsync(req.Actor, req.Intent);
        return Ok();
    }
}


public interface IRealizationService
{
    Task RealizeAsync(Actor actor, ActionIntent intent);
}

public class ConsoleRealizationService : IRealizationService
{
    public Task RealizeAsync(Actor actor, ActionIntent intent)
    {
        Console.WriteLine($"[Realize] {actor.Name}: {string.Join(", ", intent.Commands)}");
        return Task.CompletedTask;
    }
}
