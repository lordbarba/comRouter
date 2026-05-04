using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins;

/// <summary>RS232 serial port receiver.</summary>
public sealed class RS232Receiver : RS232Base, IReceiver
{
    public int WriteTimeoutMs { get; set; } = 3000;

    public bool Send(byte[] data)
    {
        if (data == null || data.Length == 0) return false;
        InitPort();
        if (_port == null) return false;
        if (!Open()) return false;
        try
        {
            _port.WriteTimeout = WriteTimeoutMs;
            _port.Write(data, 0, data.Length);
            return true;
        }
        catch { return false; }
        finally { Close(); }
    }

    public override IReadOnlyDictionary<string, string> GetConfig()
    {
        var cfg = new Dictionary<string, string>(base.GetConfig())
        {
            ["writeTimeoutMs"] = WriteTimeoutMs.ToString()
        };
        return cfg;
    }

    public override void SetConfig(IReadOnlyDictionary<string, string> config)
    {
        base.SetConfig(config);
        if (config.TryGetValue("writeTimeoutMs", out string? t) && int.TryParse(t, out int tv))
            WriteTimeoutMs = tv;
    }

    public override string ToString() => $"RS232Receiver [{PortName} {BaudRate}]";
}
