using CommRouter.Core;
using CommRouter.Interfaces.Dto;
using Microsoft.AspNetCore.Mvc;

namespace CommRouter.WebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MatchesController : ControllerBase
{
    private readonly RouterService _router;

    public MatchesController(RouterService router) => _router = router;

    [HttpGet]
    public ActionResult<IEnumerable<MatchDto>> GetAll()
        => Ok(_router.Router.Matches.Select(ToDto));

    [HttpPost]
    public ActionResult<MatchDto> Create([FromBody] CreateMatchRequest req)
    {
        var match = _router.AddMatch(
            req.Name ?? string.Empty,
            req.ListenerId,
            req.ReceiverId,
            req.Enabled,
            req.ListenerCommands ?? [],
            req.ReceiverCommands ?? []);

        if (match is null)
            return BadRequest("Listener or Receiver not found.");

        return CreatedAtAction(nameof(GetAll), null, ToDto(match));
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateMatchRequest req)
    {
        var found = _router.Router.FindMatch(id);
        if (found is null) return NotFound();

        var updated = _router.UpdateMatch(
            id,
            req.Name ?? found.Name,
            req.Enabled,
            req.ListenerCommands ?? [.. found.ListenerCommands],
            req.ReceiverCommands ?? [.. found.ReceiverCommands]);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
        => _router.RemoveMatch(id) ? NoContent() : NotFound();

    private static MatchDto ToDto(Match m) =>
        new(m.Id, m.Name, m.Enabled,
            m.Listener?.Id ?? Guid.Empty, m.Receiver?.Id ?? Guid.Empty,
            [.. m.ListenerCommands], [.. m.ReceiverCommands]);
}
