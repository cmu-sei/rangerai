using Microsoft.AspNetCore.Mvc;

namespace RangerAi.Controllers.AutomationSteps;


[ApiController]
[Route("api/automation/instruction")]
public class InstructionController : Controller
{
    private readonly IInstructionService _service;

    public InstructionController(IInstructionService service)
    {
        _service = service;
    }

    [HttpPost("parse")]
    public ActionResult<Instruction> Parse([FromBody] string input)
    {
        return Ok(_service.Parse(input));
    }
}


public interface IInstructionService
{
    Instruction Parse(string input);
}

public class RegexInstructionService : IInstructionService
{
    public Instruction Parse(string input)
    {
        var parts = input.Split(" reacts to ");
        return new Instruction
        {
            RawInput = input,
            Entities = new List<string> { parts[0].Trim() },
            Trigger = parts.Length > 1 ? parts[1].Trim() : null,
            Intent = "react"
        };
    }
}
public class Instruction
{
    public string RawInput { get; set; }                  // Original operator or system input
    public string Intent { get; set; }                    // e.g., "react", "escalate", "observe"
    public List<string> Entities { get; set; }            // Identifiers like names, roles, systems
    public string Trigger { get; set; }                   // Optional event that caused the instruction
}
