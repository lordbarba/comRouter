namespace CommRouter.Interfaces;

/// <summary>
/// Represents the message protocol used to translate and send commands.
/// </summary>
public interface IProtocol
{
    /// <summary>Sends a list of commands to a receiver, applying the protocol translation.</summary>
    bool Send(IEnumerable<string> commands, IReceiver receiver);

    /// <summary>Translates a human-readable command string to its byte array equivalent.</summary>
    byte[] GetMessage(string command);

    /// <summary>
    /// Translates a human-readable format to the internal 3-digit byte format.
    /// Supported tokens: decimal (0-255), hex (1Fh), quoted string ("AB"), p/P (pause 100ms), d/D (delay 100ms).
    /// </summary>
    string ParseCrossing(string input);
}
