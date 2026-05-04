using CommRouter.Core;
using CommRouter.Interfaces.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CommRouter.WebServer.Controllers;

/// <summary>Returns available plugin types (listener and receiver implementations).</summary>
[ApiController]
[Route("api/types")]
public sealed class TypesController : ControllerBase
{
    private readonly PluginLoader _loader;

    public TypesController(PluginLoader loader) => _loader = loader;

    [HttpGet("listeners")]
    public ActionResult<IEnumerable<PluginTypeDto>> GetListeners()
        => Ok(_loader.ListenerTypes);

    [HttpGet("receivers")]
    public ActionResult<IEnumerable<PluginTypeDto>> GetReceivers()
        => Ok(_loader.ReceiverTypes);
}
