using System.IO.Ports;
using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins;

/// <summary>RS232 serial port listener.</summary>
public sealed class RS232Listener : RS232Base, IListener
{
    private volatile bool _disposing;

    public IProtocol? Protocol { get; set; }
    public event DataReceivedHandler? DataReceived;

    public void StartListen()
    {
        InitPort();
        if (_port == null) return;
        _port.DataReceived -= Port_DataReceived;
        _port.DataReceived += Port_DataReceived;
        Open();
    }

    public void StopListen()
    {
        if (_port != null)
            _port.DataReceived -= Port_DataReceived;
        Close();
    }

    public override void Dispose()
    {
        _disposing = true;
        StopListen();
        base.Dispose();
    }

    private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_disposing || _port == null) return;
        try
        {
            int toRead = _port.BytesToRead;
            if (toRead <= 0) return;
            var buffer = new byte[toRead];
            _port.Read(buffer, 0, toRead);
            DataReceived?.Invoke(this, buffer);
        }
        catch { /* port closed or error */ }
    }

    public override string ToString() => $"RS232Listener [{PortName} {BaudRate}]";
}
