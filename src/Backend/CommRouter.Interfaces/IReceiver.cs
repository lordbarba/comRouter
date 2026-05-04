namespace CommRouter.Interfaces;

/// <summary>
/// Represents a communication receiver (RS232, TCP/IP, UDP, Process, ...).
/// </summary>
public interface IReceiver : IConfigurable, IDisposable
{
    Guid Id { get; }
    string Name { get; set; }

    bool Send(byte[] data);
}
