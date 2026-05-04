namespace CommRouter.Interfaces;

/// <summary>
/// Delegate fired when a listener receives data.
/// </summary>
public delegate void DataReceivedHandler(IListener sender, byte[] data);

/// <summary>
/// Represents a communication listener (RS232, TCP/IP, UDP, ...).
/// </summary>
public interface IListener : IConfigurable, IDisposable
{
    Guid Id { get; }
    string Name { get; set; }
    IProtocol? Protocol { get; set; }

    event DataReceivedHandler DataReceived;

    void StartListen();
    void StopListen();
}
