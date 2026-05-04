using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins;

/// <summary>Receiver that launches a process when data is received.</summary>
public sealed class ProcessReceiver : IReceiver
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ProcessPath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;

    public bool Send(byte[] data)
    {
        if (string.IsNullOrEmpty(ProcessPath)) return false;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ProcessPath, Arguments)
            {
                UseShellExecute = true,
            });
            return true;
        }
        catch { return false; }
    }

    public void Dispose() { }

    public IReadOnlyDictionary<string, string> GetConfig() => new Dictionary<string, string>
    {
        ["processPath"] = ProcessPath,
        ["arguments"]   = Arguments,
    };

    public void SetConfig(IReadOnlyDictionary<string, string> config)
    {
        if (config.TryGetValue("processPath", out string? p)) ProcessPath = p;
        if (config.TryGetValue("arguments",   out string? a)) Arguments = a;
    }

    public override string ToString() => $"ProcessReceiver [{ProcessPath}]";
}
