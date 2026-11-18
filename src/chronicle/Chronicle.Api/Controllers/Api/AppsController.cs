// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Chronicle.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Controllers.Api;

/// <summary>The Ranger apps, for the top bar's switcher, as <c>Apps</c> in appsettings.json lists them.</summary>
[Route("api/apps")]
public sealed class AppsController(IConfiguration config) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<RangerApp>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<RangerApp>> List() => Ok(config.GetSection("Apps").Get<List<RangerApp>>() ?? []);
}
