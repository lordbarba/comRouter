using CommRouter.Interfaces.Dto;
using CommRouter.WebServer.Services;
using LicenseManager.Sdk.Interfaces;
using LicenseManager.Sdk.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommRouter.WebServer.Controllers;

/// <summary>
/// Espone lo stato della licenza e i flussi di attivazione (web pickup e import .lic air-gapped).
/// Questi endpoint sono sempre accessibili anche quando la licenza non è valida
/// (esenti dal LicenseMiddleware).
/// </summary>
[ApiController]
[Route("api/license")]
public sealed class LicenseController : ControllerBase
{
    private readonly ILicenseService _license;
    private readonly LicenseState    _state;

    public LicenseController(ILicenseService license, LicenseState state)
    {
        _license = license;
        _state   = state;
    }

    /// <summary>Restituisce lo stato corrente della licenza, machine hash e URL di attivazione web.</summary>
    [HttpGet("status")]
    public async Task<LicenseStatusDto> GetStatus(CancellationToken ct)
    {
        var payload     = _license.GetCurrentPayload();
        var machineHash = await _license.GetMachineHashAsync(ct);

        string activationUrl;
        try   { activationUrl = _license.GetWebActivationUrl(); }
        catch { activationUrl = string.Empty; }

        return new LicenseStatusDto(
            IsValid:          _state.IsValid,
            Status:           MapStatus(_state.ValidationStatus, _state.IsValid, payload),
            ProductId:        _license.ProductId ?? string.Empty,
            MachineHash:      machineHash,
            WebActivationUrl: activationUrl,
            Tier:             payload?.Tier         ?? string.Empty,
            ExpiresAt:        payload?.ExpiresAtUtc.UtcDateTime,
            SerialNumber:     payload?.Serial       ?? string.Empty,
            CustomerName:     string.Empty,   // non incluso nel token; richiederebbe GetCustomerInfoAsync
            CustomerEmail:    string.Empty);
    }

    /// <summary>
    /// Verifica se l'utente ha completato l'attivazione web e, in caso positivo, salva il token localmente.
    /// </summary>
    [HttpPost("pickup")]
    public async Task<LicenseActionResultDto> Pickup(CancellationToken ct)
    {
        var result = await _license.TryPickupActivationAsync(ct);
        if (result.IsValid)
        {
            _state.IsValid          = true;
            _state.ValidationStatus = result.Status;
        }
        return new LicenseActionResultDto(result.IsValid, result.FailureReason ?? string.Empty);
    }

    /// <summary>
    /// Importa un file .lic per attivazione air-gapped.
    /// Il file viene salvato temporaneamente e poi passato all'SDK per validazione e memorizzazione.
    /// </summary>
    [HttpPost("import")]
    public async Task<LicenseActionResultDto> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return new LicenseActionResultDto(false, "Nessun file ricevuto.");

        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".lic");
        try
        {
            await using (var stream = System.IO.File.Create(tempPath))
                await file.CopyToAsync(stream, ct);

            var result = await _license.ImportLicFileAsync(tempPath, ct);
            if (result.IsValid)
            {
                _state.IsValid          = true;
                _state.ValidationStatus = result.Status;
            }
            return new LicenseActionResultDto(result.IsValid, result.FailureReason ?? string.Empty);
        }
        finally
        {
            try { System.IO.File.Delete(tempPath); } catch { /* best-effort cleanup */ }
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string MapStatus(
        LicenseValidationStatus status,
        bool isValid,
        LicenseTokenPayload? payload)
    {
        // Licenza valida ma in scadenza entro 30 giorni
        if (isValid && payload is not null &&
            payload.ExpiresAtUtc < DateTimeOffset.UtcNow.AddDays(30))
            return "ExpiringSoon";

        return status switch
        {
            LicenseValidationStatus.Valid               => "Valid",
            LicenseValidationStatus.ValidOffline        => "OfflineValid",
            LicenseValidationStatus.Expired
                or LicenseValidationStatus.OfflineWindowExpired => "Expired",
            LicenseValidationStatus.Revoked             => "Revoked",
            LicenseValidationStatus.Suspended           => "Suspended",
            LicenseValidationStatus.NotActivated
                or LicenseValidationStatus.Invalid      => "NotActivated",
            _                                           => "Unknown"
        };
    }
}
