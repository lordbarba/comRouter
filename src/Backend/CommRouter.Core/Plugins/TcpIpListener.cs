using System.Net;
using System.Net.Sockets;
using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins;

/// <summary>TCP/IP server listener. Accepts connections on the configured port and fires DataReceived for each incoming message.</summary>
public sealed class TcpIpListener : TcpIpBase, IListener
{
    private TcpListener? _server;
    private CancellationTokenSource? _cts;

    public IProtocol? Protocol { get; set; }
    public event DataReceivedHandler? DataReceived;

    public void StartListen()
    {
        StopListen();
        _cts = new CancellationTokenSource();
        _server = new TcpListener(IPAddress.Any, Port);
        _server.Start();
        _ = AcceptLoopAsync(_cts.Token);
    }

    public void StopListen()
    {
        _cts?.Cancel();
        _cts = null;
        try { _server?.Stop(); } catch { }
        _server = null;
    }

    public override void Dispose()
    {
        StopListen();
        base.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _server!.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client, ct);
            }
            catch { break; }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int read = await stream.ReadAsync(buffer, ct);
                    if (read == 0) break;
                    DataReceived?.Invoke(this, buffer[..read]);
                }
            }
            catch { }
        }
    }

    public override string ToString() => $"TcpIpListener [:{Port}]";
}
