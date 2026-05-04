using System.Text;
using CommRouter.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommRouter.Core;

/// <summary>
/// Represents a routing rule: when <see cref="Listener"/> receives a matching command,
/// sends <see cref="ReceiverCommands"/> to <see cref="Receiver"/> via the listener's protocol.
/// </summary>
public sealed class Match : IDisposable
{
    private volatile bool _disposed;
    private IListener? _listener;
    private readonly List<byte[]> _listenerMessages = [];
    private readonly ILogger<Match>? _logger;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public IReceiver? Receiver { get; set; }
    public List<string> ListenerCommands { get; private set; } = [];
    public List<string> ReceiverCommands { get; private set; } = [];

    public IListener? Listener
    {
        get => _listener;
        set => SetListener(value);
    }

    public Match() { }

    public Match(ILogger<Match>? logger) => _logger = logger;

    // ─── Commands ────────────────────────────────────────────────────────────

    public void AddListenerCommand(string command)
    {
        if (string.IsNullOrEmpty(command) || _listener?.Protocol == null) return;
        if (ListenerCommands.Contains(command)) return;
        ListenerCommands.Add(command);
        _listenerMessages.Add(_listener.Protocol.GetMessage(command));
    }

    public void RemoveListenerCommand(string command)
    {
        if (string.IsNullOrEmpty(command) || _listener?.Protocol == null) return;
        int idx = ListenerCommands.IndexOf(command);
        if (idx < 0) return;
        ListenerCommands.RemoveAt(idx);
        byte[] toRemove = _listener.Protocol.GetMessage(command);
        _listenerMessages.RemoveAll(b => b.SequenceEqual(toRemove));
    }

    public void AddReceiverCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) return;
        if (!ReceiverCommands.Contains(command))
            ReceiverCommands.Add(command);
    }

    public void RemoveReceiverCommand(string command)
    {
        ReceiverCommands.Remove(command);
    }

    public void ClearListenerCommands()
    {
        ListenerCommands.Clear();
        _listenerMessages.Clear();
    }

    public void ClearReceiverCommands() => ReceiverCommands.Clear();

    /// <summary>Restarts the listener according to the current <see cref="Enabled"/> state.</summary>
    public void RefreshListener()
    {
        if (_listener == null) return;
        _listener.StopListen();
        if (Enabled)
            _listener.StartListen();
    }

    // ─── IDisposable ─────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SetListener(null);
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    private void SetListener(IListener? listener)
    {
        if (_listener != null)
            _listener.DataReceived -= OnDataReceived;

        _listener = listener;

        if (_listener != null)
            _listener.DataReceived += OnDataReceived;

        // Rebuild pre-computed messages after listener change
        RebuildListenerMessages();
    }

    private void RebuildListenerMessages()
    {
        _listenerMessages.Clear();
        if (_listener?.Protocol == null) return;
        foreach (string cmd in ListenerCommands)
            _listenerMessages.Add(_listener.Protocol.GetMessage(cmd));
    }

    private void OnDataReceived(IListener sender, byte[] data)
    {
        if (_disposed) return;

        _logger?.LogDebug("[{MatchName}] Enabled={Enabled} — data from '{Listener}': {Hex}",
            Name, Enabled, sender.Name, ToHexString(data));

        if (!Enabled) return;
        if (!IsMatchingData(data)) return;
        if (Receiver == null || _listener?.Protocol == null)
        {
            _logger?.LogWarning("[{MatchName}] No receiver configured.", Name);
            return;
        }

        foreach (string cmd in ReceiverCommands)
            _logger?.LogDebug("[{MatchName}] Sending '{Cmd}' to '{Receiver}'", Name, cmd, Receiver.Name);

        _listener.Protocol.Send(ReceiverCommands, Receiver);
    }

    private bool IsMatchingData(byte[] data)
    {
        // If no filter commands defined, pass everything through
        if (_listenerMessages.Count == 0) return true;

        foreach (byte[] expected in _listenerMessages)
            if (expected.SequenceEqual(data)) return true;

        _logger?.LogDebug("[{MatchName}] No matching sequence found.", Name);
        return false;
    }

    private static string ToHexString(byte[] data)
    {
        if (data.Length == 0) return "(empty)";
        var sb = new StringBuilder(data.Length * 3);
        for (int i = 0; i < data.Length; i++)
        {
            sb.Append(data[i].ToString("X2"));
            if (i < data.Length - 1) sb.Append(' ');
        }
        return sb.ToString();
    }
}
