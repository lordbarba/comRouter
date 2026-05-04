using CommRouter.Core.Plugins;
using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;

namespace CommRouter.Panels;

/// <summary>
/// Factory that maps an IListener or IReceiver instance to its WinForms configuration panel.
/// </summary>
public static class ControlPanelFactory
{
    /// <summary>Returns the appropriate configuration panel for the given endpoint, or null if unknown.</summary>
    public static Control? CreatePanel(object endpoint) => endpoint switch
    {
        RS232Base rs232      => new RS232Panel(rs232),
        ProcessReceiver proc => new ProcessPanel(proc),
        TcpIpBase tcpip      => new TcpIpPanel(tcpip),
        _                    => null,
    };

    /// <inheritdoc cref="CreatePanel(object)"/>
    public static Control? CreatePanel(IListener listener) => CreatePanel((object)listener);

    /// <inheritdoc cref="CreatePanel(object)"/>
    public static Control? CreatePanel(IReceiver receiver) => CreatePanel((object)receiver);
}
