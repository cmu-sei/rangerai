using Microsoft.AspNetCore.Mvc;

namespace RangerAi.Controllers.AutomationSteps;

[ApiController]
[Route("api/automation/action")]
public class ActionController : Controller
{
    private readonly IActionService _service;

    public ActionController(IActionService service)
    {
        _service = service;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ActionIntent>> Generate([FromBody] ContextFrame frame)
    {
        var result = await _service.GenerateAsync(frame);
        return Ok(result);
    }
}

public interface IActionService
{
    Task<ActionIntent> GenerateAsync(ContextFrame context);
}

public class RuleBasedActionService : IActionService
{
    public Task<ActionIntent> GenerateAsync(ContextFrame context)
    {
        return Task.FromResult(new ActionIntent
        {
            Reasoning = $"Using static rules to respond to: {context.Trigger}",
            Commands = new List<string> { "log_event", "notify_operator" }
        });
    }
}

public class ActionIntent
{
    public string Reasoning { get; set; } // Why this action was selected/generated
    public List<string> Commands { get; set; } = new(); // Plain-text or domain-specific ops
    public Dictionary<string, object> Payload { get; set; } = new(); // Optional parameters (e.g., target info)
}
