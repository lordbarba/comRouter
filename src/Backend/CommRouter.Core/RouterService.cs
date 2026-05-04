using CommRouter.Interfaces;
using CommRouter.Interfaces.Dto;
using Microsoft.Extensions.Logging;

namespace CommRouter.Core;

/// <summary>
/// Service layer: orchestrates add/remove/start/stop operations on the <see cref="Router"/> domain model.
/// Raises <see cref="StateChanged"/> so consumers (SignalR hub, WinForms) can react to changes.
/// </summary>
public sealed class RouterService
{
    private readonly ILogger<RouterService> _logger;
    private volatile bool _running;

    public Router Router { get; } = new();

    /// <summary>Fired on any structural change (listener/receiver/match added or removed, start/stop).</summary>
    public event Action? StateChanged;

    public bool IsRunning => _running;

    public RouterService(ILogger<RouterService> logger) => _logger = logger;

    // ─── Listeners ───────────────────────────────────────────────────────────

    public void AddListener(IListener listener)
    {
        Router.AddListener(listener);
        _logger.LogInformation("Listener '{Name}' added.", listener.Name);
        StateChanged?.Invoke();
    }

    public bool RemoveListener(Guid id)
    {
        var listener = Router.FindListener(id);
        if (listener == null) return false;

        // Stop and remove dependent matches
        listener.StopListen();
        var dependentMatches = Router.Matches.Where(m => m.Listener?.Id == id).ToList();
        foreach (var m in dependentMatches)
        {
            Router.RemoveMatch(m.Id);
            m.Dispose();
            _logger.LogInformation("Match '{Name}' removed (listener removed).", m.Name);
        }

        Router.RemoveListener(id);
        listener.Dispose();
        _logger.LogInformation("Listener '{Id}' removed.", id);
        StateChanged?.Invoke();
        return true;
    }

    public bool UpdateListenerConfig(Guid id, string name, IReadOnlyDictionary<string, string> config)
    {
        var listener = Router.FindListener(id);
        if (listener == null) return false;

        bool wasListening = _running && Router.Matches.Any(m => m.Listener?.Id == id && m.Enabled);
        if (wasListening) listener.StopListen();

        listener.Name = name;
        listener.SetConfig(config);

        if (wasListening) listener.StartListen();
        StateChanged?.Invoke();
        return true;
    }

    // ─── Receivers ───────────────────────────────────────────────────────────

    public void AddReceiver(IReceiver receiver)
    {
        Router.AddReceiver(receiver);
        _logger.LogInformation("Receiver '{Name}' added.", receiver.Name);
        StateChanged?.Invoke();
    }

    public bool RemoveReceiver(Guid id)
    {
        var receiver = Router.FindReceiver(id);
        if (receiver == null) return false;

        var dependentMatches = Router.Matches.Where(m => m.Receiver?.Id == id).ToList();
        foreach (var m in dependentMatches)
        {
            Router.RemoveMatch(m.Id);
            m.Dispose();
            _logger.LogInformation("Match '{Name}' removed (receiver removed).", m.Name);
        }

        Router.RemoveReceiver(id);
        receiver.Dispose();
        _logger.LogInformation("Receiver '{Id}' removed.", id);
        StateChanged?.Invoke();
        return true;
    }

    public bool UpdateReceiverConfig(Guid id, string name, IReadOnlyDictionary<string, string> config)
    {
        var receiver = Router.FindReceiver(id);
        if (receiver == null) return false;
        receiver.Name = name;
        receiver.SetConfig(config);
        StateChanged?.Invoke();
        return true;
    }

    // ─── Matches ─────────────────────────────────────────────────────────────

    public Match? AddMatch(string name, Guid listenerId, Guid receiverId, bool enabled,
        IEnumerable<string> listenerCommands, IEnumerable<string> receiverCommands)
    {
        var listener = Router.FindListener(listenerId);
        var receiver = Router.FindReceiver(receiverId);
        if (listener == null || receiver == null) return null;

        var match = new Match(_logger as ILogger<Match>)
        {
            Name = name,
            Enabled = enabled,
            Receiver = receiver,
            Listener = listener,
        };
        foreach (string cmd in listenerCommands) match.AddListenerCommand(cmd);
        foreach (string cmd in receiverCommands) match.AddReceiverCommand(cmd);

        Router.AddMatch(match);
        _logger.LogInformation("Match '{Name}' added.", name);

        if (_running && enabled)
            listener.StartListen();

        StateChanged?.Invoke();
        return match;
    }

    public bool RemoveMatch(Guid id)
    {
        var match = Router.FindMatch(id);
        if (match == null) return false;
        match.Listener?.StopListen();
        Router.RemoveMatch(id);
        match.Dispose();
        _logger.LogInformation("Match '{Id}' removed.", id);
        StateChanged?.Invoke();
        return true;
    }

    public bool UpdateMatch(Guid id, string name, bool enabled,
        IEnumerable<string> listenerCommands, IEnumerable<string> receiverCommands)
    {
        var match = Router.FindMatch(id);
        if (match == null) return false;

        match.Name = name;
        match.Enabled = enabled;
        match.ClearListenerCommands();
        match.ClearReceiverCommands();
        foreach (string cmd in listenerCommands) match.AddListenerCommand(cmd);
        foreach (string cmd in receiverCommands) match.AddReceiverCommand(cmd);
        match.RefreshListener();
        StateChanged?.Invoke();
        return true;
    }

    // ─── Start / Stop ────────────────────────────────────────────────────────

    public void StartAll()
    {
        _running = true;
        var startedListeners = new HashSet<Guid>();

        foreach (var match in Router.Matches)
        {
            if (!match.Enabled || match.Listener == null) continue;
            if (startedListeners.Contains(match.Listener.Id)) continue;
            match.Listener.StartListen();
            startedListeners.Add(match.Listener.Id);
            _logger.LogInformation("Listener '{Name}' started.", match.Listener.Name);
        }

        StateChanged?.Invoke();
    }

    public void StopAll()
    {
        _running = false;
        var stoppedListeners = new HashSet<Guid>();

        foreach (var match in Router.Matches)
        {
            if (match.Listener == null) continue;
            if (stoppedListeners.Contains(match.Listener.Id)) continue;
            match.Listener.StopListen();
            stoppedListeners.Add(match.Listener.Id);
            _logger.LogInformation("Listener '{Name}' stopped.", match.Listener.Name);
        }

        StateChanged?.Invoke();
    }

    // ─── Projection helpers (for API / WinForms) ─────────────────────────────

    public RouterStatusDto GetStatus() => new(
        IsRunning: _running,
        ListenersCount: Router.Listeners.Count,
        ReceiversCount: Router.Receivers.Count,
        MatchesCount: Router.Matches.Count);
}
