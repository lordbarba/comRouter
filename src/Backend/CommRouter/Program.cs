using System.Diagnostics;

namespace CommRouter;

static class Program
{
    private static Process? _serverProcess;

    [STAThread]
    static void Main()
    {
        _serverProcess = TryStartWebServer();
        if (_serverProcess != null)
            WaitForWebServer();

        ApplicationConfiguration.Initialize();
        Application.Run(new frmMain());

        // Terminate the WebServer when the UI closes (installed mode only)
        try { _serverProcess?.Kill(entireProcessTree: true); } catch { }
        _serverProcess?.Dispose();
    }

    /// <summary>
    /// Avvia CommRouter.WebServer.exe dalla sottocartella "server\" se presente.
    /// In modalità sviluppo il WebServer è già avviato separatamente: restituisce null.
    /// </summary>
    private static Process? TryStartWebServer()
    {
        var serverExe = Path.Combine(AppContext.BaseDirectory, "server", "CommRouter.WebServer.exe");
        if (!File.Exists(serverExe))
            return null;

        var psi = new ProcessStartInfo(serverExe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(serverExe)!
        };
        return Process.Start(psi);
    }

    /// <summary>
    /// Attende che il WebServer risponda su /api/router/status (max <paramref name="timeoutSeconds"/> s).
    /// </summary>
    private static void WaitForWebServer(int timeoutSeconds = 30)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = http.GetAsync("http://localhost:5025/api/router/status")
                                   .GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch { }
            Thread.Sleep(300);
        }
    }
}
