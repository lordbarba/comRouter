using System.Reflection;
using CommRouter.Interfaces;
using CommRouter.Interfaces.Dto;
using Microsoft.Extensions.Logging;

namespace CommRouter.Core;

/// <summary>
/// Scans DLL files in a directory for concrete types implementing <see cref="IListener"/> or <see cref="IReceiver"/>.
/// </summary>
public sealed class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly List<PluginTypeDto> _listenerTypes = [];
    private readonly List<PluginTypeDto> _receiverTypes = [];

    public IReadOnlyList<PluginTypeDto> ListenerTypes => _listenerTypes;
    public IReadOnlyList<PluginTypeDto> ReceiverTypes => _receiverTypes;

    public PluginLoader(ILogger<PluginLoader> logger) => _logger = logger;

    /// <summary>Scans all DLLs in <paramref name="directory"/> and populates <see cref="ListenerTypes"/> and <see cref="ReceiverTypes"/>.</summary>
    public void Scan(string directory)
    {
        _listenerTypes.Clear();
        _receiverTypes.Clear();

        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Plugin directory not found: {Dir}", directory);
            return;
        }

        foreach (string dllPath in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(dllPath);
                foreach (Type type in asm.GetExportedTypes())
                {
                    if (type.IsAbstract || type.IsInterface) continue;

                    string assemblyName = asm.GetName().Name ?? asm.GetName().FullName;

                    if (typeof(IListener).IsAssignableFrom(type))
                        AddIfNotDuplicate(_listenerTypes, type, assemblyName);

                    if (typeof(IReceiver).IsAssignableFrom(type))
                        AddIfNotDuplicate(_receiverTypes, type, assemblyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugin DLL: {Path}", dllPath);
            }
        }

        _logger.LogInformation("Plugin scan complete. Listeners: {L}, Receivers: {R}",
            _listenerTypes.Count, _receiverTypes.Count);
    }

    /// <summary>Creates an instance of a type identified by assembly + full type name.</summary>
    public static object? CreateInstance(string assemblyName, string typeName)
    {
        try
        {
            return Activator.CreateInstance(assemblyName, typeName)?.Unwrap();
        }
        catch
        {
            return null;
        }
    }

    public static IListener? CreateListener(string assemblyName, string typeName) =>
        CreateInstance(assemblyName, typeName) as IListener;

    public static IReceiver? CreateReceiver(string assemblyName, string typeName) =>
        CreateInstance(assemblyName, typeName) as IReceiver;

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void AddIfNotDuplicate(List<PluginTypeDto> list, Type type, string assemblyName)
    {
        if (list.Any(t => t.TypeName == type.FullName)) return;

        List<string> configKeys = [];
        if (typeof(IConfigurable).IsAssignableFrom(type))
        {
            try
            {
                var instance = Activator.CreateInstance(type) as IConfigurable;
                if (instance != null)
                    configKeys = [.. instance.GetConfig().Keys];
            }
            catch { /* can't instantiate without args */ }
        }

        list.Add(new PluginTypeDto(
            TypeName: type.FullName ?? type.Name,
            AssemblyName: assemblyName,
            DisplayName: type.Name,
            ConfigKeys: configKeys));
    }
}
