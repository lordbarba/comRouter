using System.Net.Sockets;
using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;

namespace CommRouter.Core.Plugins;

/// <summary>UDP receiver. Sends datagrams to the configured host/port.</summary>
public sealed class UdpReceiver : TcpIpBase, IReceiver
{
    public bool Send(byte[] data)
    {
        if (data == null || data.Length == 0) return false;
        try
        {
            using var udp = new UdpClient();
            udp.Send(data, data.Length, IpAddress, Port);
            return true;
        }
        catch { return false; }
    }

    public override string ToString() => $"UdpReceiver [{IpAddress}:{Port}]";
}
