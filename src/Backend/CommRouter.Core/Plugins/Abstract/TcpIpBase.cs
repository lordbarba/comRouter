using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins.Abstract;

/// <summary>Abstract base for TCP/IP and UDP listener/receiver.</summary>
public abstract class TcpIpBase : IConfigurable, IDisposable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

    public virtual void Dispose() { }

    public virtual IReadOnlyDictionary<string, string> GetConfig() => new Dictionary<string, string>
    {
        ["ipAddress"] = IpAddress,
        ["port"]      = Port.ToString(),
    };

    public virtual void SetConfig(IReadOnlyDictionary<string, string> config)
    {
        if (config.TryGetValue("ipAddress", out string? ip)) IpAddress = ip;
        if (config.TryGetValue("port", out string? port) && int.TryParse(port, out int pv)) Port = pv;
    }
}
