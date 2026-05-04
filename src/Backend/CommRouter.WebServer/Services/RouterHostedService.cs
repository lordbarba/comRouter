using CommRouter.Core;
using CommRouter.Core.Settings;
using CommRouter.WebServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CommRouter.WebServer.Services;

/// <summary>
/// Hosted service that loads the config on startup, wires SignalR notifications,
/// and stops the router on shutdown.
/// </summary>
internal sealed class RouterHostedService : IHostedService
{
    private readonly RouterService _router;
    private readonly AppSettings _settings;
    private readonly JsonSettingsSerializer _json;
    private readonly XmlMigrationReader _xml;
    private readonly IHubContext<RouterHub> _hub;
    private readonly PluginLoader _pluginLoader;
    private readonly ILogger<RouterHostedService> _logger;

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "COMRouter", "COMRouter.json");

    private static string LegacyXmlPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "COMRouter", "COMRouter.xml");

    public RouterHostedService(
        RouterService router,
        AppSettings settings,
        JsonSettingsSerializer json,
        XmlMigrationReader xml,
        IHubContext<RouterHub> hub,
        PluginLoader pluginLoader,
        ILogger<RouterHostedService> logger)
    {
        _router = router;
        _settings = settings;
        _json = json;
        _xml = xml;
        _hub = hub;
        _pluginLoader = pluginLoader;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Discover plugin types in the app directory (finds CommRouter.Core.dll + any external plugins)
        _pluginLoader.Scan(AppContext.BaseDirectory);
        _logger.LogInformation("Plugins: {L} listeners, {R} receivers",
            _pluginLoader.ListenerTypes.Count, _pluginLoader.ReceiverTypes.Count);

        // Wire state-changed -> broadcast to all SignalR clients
        _router.StateChanged += OnStateChanged;

        // Load config: JSON preferred, fall back to XML migration
        if (File.Exists(ConfigPath))
        {
            try { _json.Load(ConfigPath, _settings, _router.Router); }
            catch (Exception ex) { _logger.LogError(ex, "Failed loading {Path}", ConfigPath); }
        }
        else if (File.Exists(LegacyXmlPath))
        {
            try
            {
                _xml.TryMigrate(LegacyXmlPath, _settings, _router.Router);
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                _json.Save(ConfigPath, _settings, _router.Router);
                _logger.LogInformation("Migrated XML config to JSON.");
            }
            catch (Exception ex) { _logger.LogError(ex, "Failed migrating XML config"); }
        }

        if (_settings.AutoStart) _router.StartAll();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _router.StateChanged -= OnStateChanged;
        _router.StopAll();
        Save();
        return Task.CompletedTask;
    }

    private void OnStateChanged()
    {
        _ = _hub.Clients.All.SendAsync("StateChanged", _router.GetStatus());
        Save();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            _json.Save(ConfigPath, _settings, _router.Router);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed saving config"); }
    }
}
