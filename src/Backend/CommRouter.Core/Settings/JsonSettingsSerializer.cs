using System.Text.Json;
using System.Text.Json.Nodes;
using CommRouter.Core;
using CommRouter.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommRouter.Core.Settings;

/// <summary>
/// Serializes and deserializes the router configuration to/from a JSON file.
/// Uses a type discriminator ("assemblyName" + "typeName") for polymorphic IListener / IReceiver instances.
/// </summary>
public sealed class JsonSettingsSerializer
{
    private readonly ILogger<JsonSettingsSerializer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JsonSettingsSerializer(ILogger<JsonSettingsSerializer> logger) => _logger = logger;

    // ─── Save ─────────────────────────────────────────────────────────────────

    public bool Save(string filePath, AppSettings appSettings, Router router)
    {
        try
        {
            var root = new JsonObject
            {
                ["autoStart"] = appSettings.AutoStart,
            };

            // Rewrite listeners/receivers with cleaner approach
            root["listeners"] = new JsonArray(router.Listeners.Select(SerializeEndpoint).ToArray()!);
            root["receivers"] = new JsonArray(router.Receivers.Select(SerializeEndpoint).ToArray()!);
            root["matches"] = new JsonArray(router.Matches.Select(SerializeMatch).ToArray()!);

            string json = root.ToJsonString(JsonOptions);
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, json);
            _logger.LogInformation("Settings saved to {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", filePath);
            return false;
        }
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    public bool Load(string filePath, AppSettings appSettings, Router router)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Settings file not found: {Path}", filePath);
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root == null) return false;

            if (root["autoStart"] is JsonValue autoStartVal)
                appSettings.AutoStart = autoStartVal.GetValue<bool>();

            // Listeners
            if (root["listeners"] is JsonArray listenersArr)
            {
                foreach (var node in listenersArr)
                {
                    var listener = DeserializeListener(node?.AsObject());
                    if (listener != null) router.AddListener(listener);
                }
            }

            // Receivers
            if (root["receivers"] is JsonArray receiversArr)
            {
                foreach (var node in receiversArr)
                {
                    var receiver = DeserializeReceiver(node?.AsObject());
                    if (receiver != null) router.AddReceiver(receiver);
                }
            }

            // Matches
            if (root["matches"] is JsonArray matchesArr)
            {
                foreach (var node in matchesArr)
                {
                    var match = DeserializeMatch(node?.AsObject(), router);
                    if (match != null) router.AddMatch(match);
                }
            }

            _logger.LogInformation("Settings loaded from {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", filePath);
            return false;
        }
    }

    // ─── Serialization helpers ────────────────────────────────────────────────

    private static JsonObject SerializeEndpoint(object endpoint)
    {
        var type = endpoint.GetType();
        var cfg = (IConfigurable)endpoint;
        var id = endpoint is IListener l ? l.Id : ((IReceiver)endpoint).Id;
        var name = endpoint is IListener ll ? ll.Name : ((IReceiver)endpoint).Name;

        var node = new JsonObject
        {
            ["id"] = id.ToString(),
            ["name"] = name,
            ["assemblyName"] = type.Assembly.GetName().Name,
            ["typeName"] = type.FullName,
            ["config"] = JsonObject.Create(JsonSerializer.SerializeToElement(
                cfg.GetConfig(), JsonOptions)),
        };
        return node;
    }

    private static JsonObject SerializeMatch(Match match)
    {
        var node = new JsonObject
        {
            ["id"] = match.Id.ToString(),
            ["name"] = match.Name,
            ["enabled"] = match.Enabled,
            ["listenerId"] = match.Listener?.Id.ToString(),
            ["receiverId"] = match.Receiver?.Id.ToString(),
            ["listenerCommands"] = new JsonArray(match.ListenerCommands.Select(c => JsonValue.Create(c)).ToArray()!),
            ["receiverCommands"] = new JsonArray(match.ReceiverCommands.Select(c => JsonValue.Create(c)).ToArray()!),
        };
        return node;
    }

    private static IListener? DeserializeListener(JsonObject? obj)
    {
        if (obj == null) return null;
        string? assemblyName = obj["assemblyName"]?.GetValue<string>();
        string? typeName = obj["typeName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(typeName)) return null;

        var listener = PluginLoader.CreateListener(assemblyName, typeName);
        if (listener == null) return null;

        if (obj["id"]?.GetValue<string>() is string idStr && Guid.TryParse(idStr, out Guid id))
        {
            // Id is init-only, so we set it via the concrete type's field
            // Concrete types expose Id as init — we use a workaround via reflection
            TrySetId(listener, id);
        }

        if (obj["name"]?.GetValue<string>() is string name) listener.Name = name;
        if (obj["config"] is JsonObject configNode)
        {
            var config = configNode.ToDictionary(kv => kv.Key, kv => kv.Value?.GetValue<string>() ?? "");
            listener.SetConfig(config);
        }

        return listener;
    }

    private static IReceiver? DeserializeReceiver(JsonObject? obj)
    {
        if (obj == null) return null;
        string? assemblyName = obj["assemblyName"]?.GetValue<string>();
        string? typeName = obj["typeName"]?.GetValue<string>();
        if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(typeName)) return null;

        var receiver = PluginLoader.CreateReceiver(assemblyName, typeName);
        if (receiver == null) return null;

        if (obj["id"]?.GetValue<string>() is string idStr && Guid.TryParse(idStr, out Guid id))
            TrySetId(receiver, id);

        if (obj["name"]?.GetValue<string>() is string name) receiver.Name = name;
        if (obj["config"] is JsonObject configNode)
        {
            var config = configNode.ToDictionary(kv => kv.Key, kv => kv.Value?.GetValue<string>() ?? "");
            receiver.SetConfig(config);
        }

        return receiver;
    }

    private static Match? DeserializeMatch(JsonObject? obj, Router router)
    {
        if (obj == null) return null;

        var match = new Match
        {
            Name = obj["name"]?.GetValue<string>() ?? string.Empty,
            Enabled = obj["enabled"]?.GetValue<bool>() ?? false,
        };

        if (obj["id"]?.GetValue<string>() is string idStr && Guid.TryParse(idStr, out Guid id))
            TrySetId(match, id, nameof(Match.Id));

        if (obj["listenerId"]?.GetValue<string>() is string lIdStr && Guid.TryParse(lIdStr, out Guid lId))
            match.Listener = router.FindListener(lId);

        if (obj["receiverId"]?.GetValue<string>() is string rIdStr && Guid.TryParse(rIdStr, out Guid rId))
            match.Receiver = router.FindReceiver(rId);

        if (obj["listenerCommands"] is JsonArray lCmds)
            foreach (var cmd in lCmds) match.AddListenerCommand(cmd?.GetValue<string>() ?? "");

        if (obj["receiverCommands"] is JsonArray rCmds)
            foreach (var cmd in rCmds) match.AddReceiverCommand(cmd?.GetValue<string>() ?? "");

        return match;
    }

    private static void TrySetId(object target, Guid id, string propertyName = "Id")
    {
        // Init-only properties can still be set via reflection on backing field
        var prop = target.GetType().GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop?.CanWrite == true)
            prop.SetValue(target, id);
        else
        {
            // Try backing field for init-only
            var field = target.GetType().GetField($"<{propertyName}>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, id);
        }
    }

    // Unused overload kept for compatibility
    private static JsonArray SerializeComponents<T>(IEnumerable<T> items) => new();
}
