using LicenseManager.Sdk.Models;

namespace CommRouter.WebServer.Services;

/// <summary>
/// Singleton che tiene traccia dello stato di validazione della licenza,
/// aggiornato all'avvio e ad ogni evento StatusChanged.
/// Letto dal LicenseMiddleware senza I/O.
/// </summary>
public sealed class LicenseState
{
    public bool IsValid { get; set; }
    public LicenseValidationStatus ValidationStatus { get; set; } = LicenseValidationStatus.NotActivated;
}
