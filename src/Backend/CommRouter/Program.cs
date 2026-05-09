using CommRouter.Interfaces.Dto;
using JBFormsLibrary.licenseForm;
using System.Diagnostics;
using System.Net.Http.Json;

namespace CommRouter;

static class Program
{
    private static readonly Uri WebServerBase = new("http://localhost:5025");
    private static Process? _serverProcess;

    [STAThread]
    static void Main()
    {
        _serverProcess = TryStartWebServer();
        if (_serverProcess != null)
            WaitForWebServer();

        ApplicationConfiguration.Initialize();

        if (!CheckLicense())
            return; // licenza non attivata → chiudi senza aprire frmMain

        Application.Run(new frmMain());

        // Terminate the WebServer when the UI closes (installed mode only)
        try { _serverProcess?.Kill(entireProcessTree: true); } catch { }
        _serverProcess?.Dispose();
    }

    // ─── License check ────────────────────────────────────────────────────────

    /// <summary>
    /// Controlla la licenza sul WebServer. Se non è valida mostra frmJBLicenseInfo
    /// per consentire l'attivazione. Restituisce true solo se la licenza è valida al termine.
    /// </summary>
    private static bool CheckLicense()
    {
        using var http = new HttpClient { BaseAddress = WebServerBase };

        var status = GetLicenseStatus(http);
        if (status is null)
            return true; // server non raggiungibile dopo il wait → lascia procedere

        if (status.IsValid)
            return true;

        // Mostra dialog di attivazione
        ShowLicenseDialog(http, status);

        // Ri-verifica dopo che l'utente ha chiuso il dialog
        var updated = GetLicenseStatus(http);
        return updated?.IsValid == true;
    }

    private static LicenseStatusDto? GetLicenseStatus(HttpClient http)
    {
        try
        {
            return http.GetFromJsonAsync<LicenseStatusDto>("/api/license/status")
                       .GetAwaiter().GetResult();
        }
        catch { return null; }
    }

    private static void ShowLicenseDialog(HttpClient http, LicenseStatusDto status)
    {
        var info = new JBLicenseInfo
        {
            ProductId    = status.ProductId,
            ProductName  = "ComRouter",
            MachineHash  = status.MachineHash,
            IsActivated  = status.IsValid,
            Status       = MapStatus(status.Status),
            ExpiresAt    = status.ExpiresAt,
            Tier         = status.Tier,
            SerialNumber = status.SerialNumber,
            CustomerName = status.CustomerName,
            CustomerEmail = status.CustomerEmail,
        };

        var actions = new JBLicenseActions
        {
            // Il server ha già pre-costruito l'URL con productId + machineHash;
            // il serial immesso dall'utente viene ignorato perché viene inserito sulla pagina web.
            GetWebActivationUrl = _ => status.WebActivationUrl,

            TryPickupActivation = async ct =>
            {
                var result = await http.PostAsync("/api/license/pickup", null, ct);
                if (!result.IsSuccessStatusCode) return false;
                var dto = await result.Content.ReadFromJsonAsync<LicenseActionResultDto>(ct);
                return dto?.Success == true;
            },

            ImportLicFile = async (path, ct) =>
            {
                await using var fs      = File.OpenRead(path);
                using var       content = new MultipartFormDataContent();
                content.Add(new StreamContent(fs), "file", Path.GetFileName(path));
                var result = await http.PostAsync("/api/license/import", content, ct);
                if (!result.IsSuccessStatusCode) return false;
                var dto = await result.Content.ReadFromJsonAsync<LicenseActionResultDto>(ct);
                return dto?.Success == true;
            },
        };

        using var frm = new frmJBLicenseInfo();
        frm.SetLicense(info, actions);
        frm.ShowDialog();
    }

    private static JBLicenseStatus MapStatus(string status) => status switch
    {
        "Valid"          => JBLicenseStatus.Valid,
        "ExpiringSoon"   => JBLicenseStatus.ExpiringSoon,
        "OfflineValid"   => JBLicenseStatus.OfflineValid,
        "Expired"        => JBLicenseStatus.Expired,
        "Revoked"        => JBLicenseStatus.Revoked,
        "Suspended"      => JBLicenseStatus.Suspended,
        "NotActivated"   => JBLicenseStatus.NotActivated,
        _                => JBLicenseStatus.Unknown,
    };

    // ─── WebServer startup helpers ────────────────────────────────────────────

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
            UseShellExecute  = false,
            CreateNoWindow   = true,
            WorkingDirectory = Path.GetDirectoryName(serverExe)!
        };
        return Process.Start(psi);
    }

    /// <summary>
    /// Attende che il WebServer risponda su /api/router/status (max <paramref name="timeoutSeconds"/> s).
    /// </summary>
    private static void WaitForWebServer(int timeoutSeconds = 30)
    {
        using var http     = new HttpClient();
        var       deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
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

