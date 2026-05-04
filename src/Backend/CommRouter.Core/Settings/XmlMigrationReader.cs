using System.Xml;
using CommRouter.Core;
using CommRouter.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommRouter.Core.Settings;

/// <summary>
/// Read-only migration reader for the legacy XML configuration format.
/// Reads the old COMRouter.xml and populates the Router + AppSettings.
/// After migration, call <see cref="JsonSettingsSerializer.Save"/> to persist as JSON.
/// </summary>
public sealed class XmlMigrationReader
{
    private readonly ILogger<XmlMigrationReader> _logger;

    public XmlMigrationReader(ILogger<XmlMigrationReader> logger) => _logger = logger;

    public bool TryMigrate(string xmlFilePath, AppSettings appSettings, Router router)
    {
        if (!File.Exists(xmlFilePath))
        {
            _logger.LogWarning("XML migration file not found: {Path}", xmlFilePath);
            return false;
        }

        try
        {
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);

            // AutoStart
            var autoStartNode = doc.GetElementsByTagName("AutoStart").Item(0);
            if (autoStartNode != null && int.TryParse(autoStartNode.InnerText, out int av))
                appSettings.AutoStart = av == 1 || av == -1;

            var routerNode = doc.GetElementsByTagName("Router").Item(0) as XmlElement;
            if (routerNode == null) return false;

            // Listeners
            LoadEndpoints<IListener>(routerNode, "Listeners", "Listener",
                (asm, type) => PluginLoader.CreateListener(asm, type),
                l => router.AddListener(l));

            // Receivers
            LoadEndpoints<IReceiver>(routerNode, "Receivers", "Receiver",
                (asm, type) => PluginLoader.CreateReceiver(asm, type),
                r => router.AddReceiver(r));

            // Matches
            LoadMatches(routerNode, router);

            _logger.LogInformation("XML migration completed from {Path}", xmlFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "XML migration failed for {Path}", xmlFilePath);
            return false;
        }
    }

    private static void LoadEndpoints<T>(XmlElement routerNode, string parentTag, string childTag,
        Func<string, string, T?> factory, Action<T> add) where T : class
    {
        var parentNode = routerNode.GetElementsByTagName(parentTag).Item(0) as XmlElement;
        if (parentNode == null) return;

        foreach (XmlElement elem in parentNode.GetElementsByTagName(childTag))
        {
            string asm = elem.GetElementsByTagName("Assembly").Item(0)?.InnerText ?? string.Empty;
            string type = elem.GetElementsByTagName("Type").Item(0)?.InnerText ?? string.Empty;
            if (string.IsNullOrEmpty(asm) || string.IsNullOrEmpty(type)) continue;

            var instance = factory(asm, type);
            if (instance == null) continue;

            // Apply config from XML attributes
            if (instance is IConfigurable cfg)
            {
                var config = new Dictionary<string, string>();
                foreach (XmlElement child in elem.ChildNodes.OfType<XmlElement>())
                {
                    if (child.Name is "Assembly" or "Type") continue;
                    config[ToCamelCase(child.Name)] = child.InnerText;
                }
                cfg.SetConfig(config);
            }

            add(instance);
        }
    }

    private static void LoadMatches(XmlElement routerNode, Router router)
    {
        var matchesNode = routerNode.GetElementsByTagName("Matches").Item(0) as XmlElement;
        if (matchesNode == null) return;

        foreach (XmlElement elem in matchesNode.GetElementsByTagName("Match"))
        {
            var match = new Match
            {
                Name = elem.GetElementsByTagName("Name").Item(0)?.InnerText ?? string.Empty,
                Enabled = (elem.GetElementsByTagName("Enabled").Item(0)?.InnerText is string en
                    && int.TryParse(en, out int ev) && (ev == 1 || ev == -1)),
            };

            // Resolve listener
            string? listenerGuidStr = elem.GetElementsByTagName("Listener").Item(0)?.InnerText;
            if (listenerGuidStr != null)
            {
                foreach (var l in router.Listeners)
                {
                    if (l.Id.ToString().Equals(listenerGuidStr, StringComparison.OrdinalIgnoreCase))
                    { match.Listener = l; break; }
                }
            }

            // Resolve receiver
            string? receiverGuidStr = elem.GetElementsByTagName("Receiver").Item(0)?.InnerText;
            if (receiverGuidStr != null)
            {
                foreach (var r in router.Receivers)
                {
                    if (r.Id.ToString().Equals(receiverGuidStr, StringComparison.OrdinalIgnoreCase))
                    { match.Receiver = r; break; }
                }
            }

            // Commands
            if (elem.GetElementsByTagName("ListenerCommands").Item(0) is XmlElement lcParent)
                foreach (XmlElement cmd in lcParent.GetElementsByTagName("Command"))
                    match.AddListenerCommand(cmd.InnerText);

            if (elem.GetElementsByTagName("ReceiverCommands").Item(0) is XmlElement rcParent)
                foreach (XmlElement cmd in rcParent.GetElementsByTagName("Command"))
                    match.AddReceiverCommand(cmd.InnerText);

            router.AddMatch(match);
        }
    }

    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLower(s[0]) + s[1..];
}
