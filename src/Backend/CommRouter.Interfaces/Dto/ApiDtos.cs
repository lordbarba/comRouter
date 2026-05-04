namespace CommRouter.Interfaces.Dto;

public record ListenerDto(
    Guid Id,
    string Name,
    string TypeName,
    string AssemblyName,
    IReadOnlyDictionary<string, string> Config,
    bool IsListening);

public record ReceiverDto(
    Guid Id,
    string Name,
    string TypeName,
    string AssemblyName,
    IReadOnlyDictionary<string, string> Config);

public record MatchDto(
    Guid Id,
    string Name,
    bool Enabled,
    Guid ListenerId,
    Guid ReceiverId,
    List<string> ListenerCommands,
    List<string> ReceiverCommands);

public record RouterStatusDto(
    bool IsRunning,
    int ListenersCount,
    int ReceiversCount,
    int MatchesCount);

public record PluginTypeDto(
    string TypeName,
    string AssemblyName,
    string DisplayName,
    List<string> ConfigKeys);

// Log streaming
public record LogEntryDto(string Timestamp, string Level, string Message);

// Request bodies
public record CreateListenerRequest(string Name, string TypeName, string AssemblyName, Dictionary<string, string> Config);
public record UpdateListenerRequest(string Name, Dictionary<string, string> Config);
public record CreateReceiverRequest(string Name, string TypeName, string AssemblyName, Dictionary<string, string> Config);
public record UpdateReceiverRequest(string Name, Dictionary<string, string> Config);
public record CreateMatchRequest(string Name, Guid ListenerId, Guid ReceiverId, bool Enabled, List<string> ListenerCommands, List<string> ReceiverCommands);
public record UpdateMatchRequest(string Name, bool Enabled, List<string> ListenerCommands, List<string> ReceiverCommands);
