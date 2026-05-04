using System.Net.Sockets;
using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins;

/// <summary>UDP listener. Receives datagrams on the configured port.</summary>
public sealed class UdpListener : TcpIpBase, IListener
{
    private UdpClient? _udp;
    private CancellationTokenSource? _cts;

    public IProtocol? Protocol { get; set; }
    public event DataReceivedHandler? DataReceived;

    public void StartListen()
    {
        StopListen();
        _cts = new CancellationTokenSource();
        _udp = new UdpClient(Port);
        _ = ReceiveLoopAsync(_cts.Token);
    }

    public void StopListen()
    {
        _cts?.Cancel();
        _cts = null;
        try { _udp?.Close(); } catch { }
        _udp = null;
    }

    public override void Dispose()
    {
        StopListen();
        base.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp!.ReceiveAsync(ct);
                DataReceived?.Invoke(this, result.Buffer);
            }
            catch { break; }
        }
    }

    public override string ToString() => $"UdpListener [:{Port}]";
}
