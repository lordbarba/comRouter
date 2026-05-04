using System.Text;
using CommRouter.Interfaces;

namespace CommRouter.Core;

/// <summary>
/// Default implementation of IProtocol.
/// Translates human-readable command strings (e.g. "56h,23,"AB",p,d") to byte sequences.
/// Special tokens: p/P and d/D insert a 100ms pause between byte groups.
/// </summary>
public sealed class AppProtocol : IProtocol
{
    // Internal encoding: each byte is represented as 3-digit decimal string (000–255).
    // Special codes: "256" = pause, "257" = delay.
    private const string PauseToken = "256";
    private const string DelayToken = "257";
    private const int PauseDelayMs = 100;

    public bool Send(IEnumerable<string> commands, IReceiver receiver)
    {
        if (receiver == null) return false;
        bool result = true;
        bool any = false;
        foreach (string cmd in commands)
        {
            any = true;
            result &= SendMessage(cmd, receiver);
        }
        if (!any)
            result = receiver.Send([]);
        return result;
    }

    public byte[] GetMessage(string command)
    {
        string encoded = ParseCrossing(command);
        var bytes = new List<byte>();
        ReadOnlySpan<char> span = encoded;
        while (span.Length >= 3)
        {
            string token = new(span[..3]);
            span = span[3..];
            if (token == PauseToken || token == DelayToken) continue;
            if (byte.TryParse(token, out byte b))
                bytes.Add(b);
        }
        return [.. bytes];
    }

    public string ParseCrossing(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder();
        ReadOnlySpan<char> remaining = input.AsSpan().Trim();

        while (!remaining.IsEmpty)
        {
            int commaIndex = remaining.IndexOf(',');
            ReadOnlySpan<char> token;
            if (commaIndex >= 0)
            {
                token = remaining[..commaIndex].Trim();
                remaining = remaining[(commaIndex + 1)..].Trim();
            }
            else
            {
                token = remaining.Trim();
                remaining = ReadOnlySpan<char>.Empty;
            }

            if (token.IsEmpty) continue;

            if (token[0] == '"' || token[^1] == '"')
            {
                // Quoted string — each char becomes its ASCII decimal
                var str = token.ToString().Replace("\"", "").AsSpan();
                foreach (char c in str)
                    sb.Append(((byte)c).ToString("D3"));
            }
            else if (token.Contains('H') || token.Contains('h'))
            {
                // Hex token: "1Fh" or "h1F"
                string s = token.ToString().ToUpper();
                int hPos = s.IndexOf('H');
                string hexStr = hPos == 0 ? s[1..] : s[..hPos];
                if (byte.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out byte hb))
                    sb.Append(hb.ToString("D3"));
            }
            else if (token.Equals("p", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(PauseToken);
            }
            else if (token.Equals("d", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(DelayToken);
            }
            else
            {
                if (byte.TryParse(token, out byte db))
                    sb.Append(db.ToString("D3"));
            }
        }

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────────────────────────────────

    private bool SendMessage(string command, IReceiver receiver)
    {
        string encoded = ParseCrossing(command);
        var pendingBytes = new List<byte>();
        bool result = true;
        ReadOnlySpan<char> span = encoded;

        while (span.Length >= 3)
        {
            string token = new(span[..3]);
            span = span[3..];

            if (token == PauseToken || token == DelayToken)
            {
                if (pendingBytes.Count > 0)
                {
                    result &= receiver.Send([.. pendingBytes]);
                    pendingBytes.Clear();
                }
                Thread.Sleep(PauseDelayMs);
            }
            else if (byte.TryParse(token, out byte b))
            {
                pendingBytes.Add(b);
            }
        }

        if (pendingBytes.Count > 0)
            result &= receiver.Send([.. pendingBytes]);

        return result;
    }
}
