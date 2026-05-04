using CommRouter.Core;
using CommRouter.Interfaces;
using CommRouter.Interfaces.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CommRouter.WebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ListenersController : ControllerBase
{
    private readonly RouterService _router;
    private readonly PluginLoader _loader;

    public ListenersController(RouterService router, PluginLoader loader)
    {
        _router = router;
        _loader = loader;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ListenerDto>> GetAll()
        => Ok(_router.Router.Listeners.Select(ToDto));

    [HttpPost]
    public ActionResult<ListenerDto> Create([FromBody] CreateListenerRequest req)
    {
        var listener = PluginLoader.CreateListener(req.AssemblyName, req.TypeName)
            ?? throw new InvalidOperationException($"Type {req.TypeName} not found in {req.AssemblyName}.");
        if (!string.IsNullOrEmpty(req.Name)) listener.Name = req.Name;
        if (req.Config is { Count: > 0 }) listener.SetConfig(req.Config);
        _router.AddListener(listener);
        return CreatedAtAction(nameof(GetAll), null, ToDto(listener));
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateListenerRequest req)
    {
        var found = _router.Router.FindListener(id);
        if (found is null) return NotFound();
        _router.UpdateListenerConfig(id, req.Name ?? found.Name, req.Config ?? found.GetConfig());
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var found = _router.Router.FindListener(id);
        if (found is null) return NotFound();
        _router.RemoveListener(id);
        return NoContent();
    }

    private static ListenerDto ToDto(IListener l) =>
        new(l.Id, l.Name, l.GetType().FullName!, l.GetType().Assembly.Location, l.GetConfig(), false);
}
