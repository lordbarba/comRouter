using CommRouter.Core;
using CommRouter.Interfaces;
using CommRouter.Interfaces.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CommRouter.WebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReceiversController : ControllerBase
{
    private readonly RouterService _router;
    private readonly PluginLoader _loader;

    public ReceiversController(RouterService router, PluginLoader loader)
    {
        _router = router;
        _loader = loader;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ReceiverDto>> GetAll()
        => Ok(_router.Router.Receivers.Select(ToDto));

    [HttpPost]
    public ActionResult<ReceiverDto> Create([FromBody] CreateReceiverRequest req)
    {
        var receiver = PluginLoader.CreateReceiver(req.AssemblyName, req.TypeName)
            ?? throw new InvalidOperationException($"Type {req.TypeName} not found in {req.AssemblyName}.");
        if (!string.IsNullOrEmpty(req.Name)) receiver.Name = req.Name;
        if (req.Config is { Count: > 0 }) receiver.SetConfig(req.Config);
        _router.AddReceiver(receiver);
        return CreatedAtAction(nameof(GetAll), null, ToDto(receiver));
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateReceiverRequest req)
    {
        var found = _router.Router.FindReceiver(id);
        if (found is null) return NotFound();
        _router.UpdateReceiverConfig(id, req.Name ?? found.Name, req.Config ?? found.GetConfig());
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var found = _router.Router.FindReceiver(id);
        if (found is null) return NotFound();
        _router.RemoveReceiver(id);
        return NoContent();
    }

    private static ReceiverDto ToDto(IReceiver r) =>
        new(r.Id, r.Name, r.GetType().FullName!, r.GetType().Assembly.Location, r.GetConfig());
}
