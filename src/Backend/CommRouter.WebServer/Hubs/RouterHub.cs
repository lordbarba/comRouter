using Microsoft.AspNetCore.SignalR;

namespace CommRouter.WebServer.Hubs;

/// <summary>SignalR hub for real-time router state and log streaming.</summary>
public sealed class RouterHub : Hub
{
    // Clients subscribe to "StateChanged" and "LogEntry" events.
    // The server broadcasts via IHubContext<RouterHub>.
}
