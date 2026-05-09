using CommRouter.WebServer.Services;

namespace CommRouter.WebServer.Middleware;

/// <summary>
/// Restituisce 402 Payment Required su tutti gli endpoint /api/* non-license
/// quando la licenza non è valida o non è ancora attivata.
/// Sono sempre esenti: /api/license/*, /hubs/*, /swagger/* e tutti i path statici/SPA.
/// </summary>
internal sealed class LicenseMiddleware
{
    private readonly RequestDelegate _next;

    public LicenseMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, LicenseState licenseState)
    {
        if (IsExempt(context.Request.Path) || licenseState.IsValid)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode  = StatusCodes.Status402PaymentRequired;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            "{\"error\":\"License not valid or not activated. Use GET /api/license/status to activate.\"}");
    }

    private static bool IsExempt(PathString path) =>
        path.StartsWithSegments("/api/license") ||
        path.StartsWithSegments("/hubs")        ||
        path.StartsWithSegments("/swagger")     ||
        !path.StartsWithSegments("/api");        // static files, SPA, health checks…
}
