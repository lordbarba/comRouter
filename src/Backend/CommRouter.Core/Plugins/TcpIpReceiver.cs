using System.Net.Sockets;
using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins;

/// <summary>TCP/IP client receiver. Connects to the configured host/port and sends data.</summary>
public sealed class TcpIpReceiver : TcpIpBase, IReceiver
{
    public int SendTimeoutMs { get; set; } = 3000;

    public bool Send(byte[] data)
    {
        if (data == null || data.Length == 0) return false;
        try
        {
            using var client = new TcpClient();
            client.SendTimeout = SendTimeoutMs;
            client.Connect(IpAddress, Port);
            using var stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            return true;
        }
        catch { return false; }
    }

    public override IReadOnlyDictionary<string, string> GetConfig()
    {
        var cfg = new Dictionary<string, string>(base.GetConfig())
        {
            ["sendTimeoutMs"] = SendTimeoutMs.ToString()
        };
        return cfg;
    }

    public override void SetConfig(IReadOnlyDictionary<string, string> config)
    {
        base.SetConfig(config);
        if (config.TryGetValue("sendTimeoutMs", out string? t) && int.TryParse(t, out int tv))
            SendTimeoutMs = tv;
    }

    public override string ToString() => $"TcpIpReceiver [{IpAddress}:{Port}]";
}
