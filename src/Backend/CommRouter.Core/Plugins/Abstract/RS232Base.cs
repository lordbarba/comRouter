using System.IO.Ports;
using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins.Abstract;

/// <summary>Abstract base for RS232 listener and receiver. Manages the SerialPort lifecycle.</summary>
public abstract class RS232Base : IConfigurable, IDisposable
{
    protected SerialPort? _port;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Parity Parity { get; set; } = Parity.None;
    public Handshake Handshake { get; set; } = Handshake.None;

    protected void InitPort()
    {
        DisposePort();
        _port = new SerialPort
        {
            PortName = PortName,
            BaudRate = BaudRate,
            DataBits = DataBits,
            StopBits = StopBits,
            Parity = Parity,
            Handshake = Handshake,
        };
    }

    protected bool Open()
    {
        if (_port == null) return false;
        if (_port.IsOpen) return true;
        try { _port.Open(); return true; }
        catch { return false; }
    }

    protected bool Close()
    {
        if (_port == null) return false;
        try { if (_port.IsOpen) _port.Close(); return true; }
        catch { return false; }
    }

    protected void DisposePort()
    {
        if (_port == null) return;
        try
        {
            if (_port.IsOpen) _port.Close();
            _port.Dispose();
        }
        finally { _port = null; }
    }

    public virtual void Dispose() => DisposePort();

    public virtual IReadOnlyDictionary<string, string> GetConfig() => new Dictionary<string, string>
    {
        ["portName"]   = PortName,
        ["baudRate"]   = BaudRate.ToString(),
        ["dataBits"]   = DataBits.ToString(),
        ["stopBits"]   = ((int)StopBits).ToString(),
        ["parity"]     = ((int)Parity).ToString(),
        ["handshake"]  = ((int)Handshake).ToString(),
    };

    public virtual void SetConfig(IReadOnlyDictionary<string, string> config)
    {
        if (config.TryGetValue("portName",  out string? pn)) PortName = pn;
        if (config.TryGetValue("baudRate",  out string? br) && int.TryParse(br, out int brv)) BaudRate = brv;
        if (config.TryGetValue("dataBits",  out string? db) && int.TryParse(db, out int dbv)) DataBits = dbv;
        if (config.TryGetValue("stopBits",  out string? sb) && int.TryParse(sb, out int sbv)) StopBits = (StopBits)sbv;
        if (config.TryGetValue("parity",    out string? pa) && int.TryParse(pa, out int pav)) Parity = (Parity)pav;
        if (config.TryGetValue("handshake", out string? hs) && int.TryParse(hs, out int hsv)) Handshake = (Handshake)hsv;
    }
}
