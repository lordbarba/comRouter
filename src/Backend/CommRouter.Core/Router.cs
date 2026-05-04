using CommRouter.Interfaces;

namespace CommRouter.Core;

/// <summary>
/// Pure domain model: holds the lists of listeners, receivers and matches.
/// No side-effects — orchestration is in <see cref="RouterService"/>.
/// </summary>
public sealed class Router
{
    private readonly List<IListener> _listeners = [];
    private readonly List<IReceiver> _receivers = [];
    private readonly List<Match> _matches = [];

    public IReadOnlyList<IListener> Listeners => _listeners;
    public IReadOnlyList<IReceiver> Receivers => _receivers;
    public IReadOnlyList<Match> Matches => _matches;

    // ─── Listeners ───────────────────────────────────────────────────────────

    public void AddListener(IListener listener) => _listeners.Add(listener);

    public bool RemoveListener(Guid id)
    {
        var l = _listeners.FirstOrDefault(x => x.Id == id);
        if (l == null) return false;
        _listeners.Remove(l);
        return true;
    }

    public IListener? FindListener(Guid id) => _listeners.FirstOrDefault(x => x.Id == id);

    // ─── Receivers ───────────────────────────────────────────────────────────

    public void AddReceiver(IReceiver receiver) => _receivers.Add(receiver);

    public bool RemoveReceiver(Guid id)
    {
        var r = _receivers.FirstOrDefault(x => x.Id == id);
        if (r == null) return false;
        _receivers.Remove(r);
        return true;
    }

    public IReceiver? FindReceiver(Guid id) => _receivers.FirstOrDefault(x => x.Id == id);

    // ─── Matches ─────────────────────────────────────────────────────────────

    public void AddMatch(Match match) => _matches.Add(match);

    public bool RemoveMatch(Guid id)
    {
        var m = _matches.FirstOrDefault(x => x.Id == id);
        if (m == null) return false;
        _matches.Remove(m);
        return true;
    }

    public Match? FindMatch(Guid id) => _matches.FirstOrDefault(x => x.Id == id);

    public Match? FindMatch(Guid listenerId, Guid receiverId) =>
        _matches.FirstOrDefault(m => m.Listener?.Id == listenerId && m.Receiver?.Id == receiverId);
}
