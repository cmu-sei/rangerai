using Microsoft.AspNetCore.Mvc;

namespace RangerAi.Controllers;

[Route("/")]
public class ApplicationController : Controller
{
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("detail")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Detail()
    {
        return View();
    }
}
