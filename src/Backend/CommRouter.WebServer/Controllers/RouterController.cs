using CommRouter.Core;
using CommRouter.Interfaces.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CommRouter.WebServer.Controllers;

[ApiController]
[Route("api/router")]
public sealed class RouterController : ControllerBase
{
    private readonly RouterService _router;

    public RouterController(RouterService router) => _router = router;

    [HttpGet("status")]
    public ActionResult<RouterStatusDto> GetStatus() => Ok(_router.GetStatus());

    [HttpPost("start")]
    public IActionResult Start()
    {
        _router.StartAll();
        return NoContent();
    }

    [HttpPost("stop")]
    public IActionResult Stop()
    {
        _router.StopAll();
        return NoContent();
    }
}
