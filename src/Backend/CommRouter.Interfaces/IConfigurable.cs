namespace CommRouter.Interfaces;

/// <summary>
/// Provides configuration get/set capability for serializable components.
/// </summary>
public interface IConfigurable
{
    /// <summary>Returns a flat dictionary of the current configuration (property name → string value).</summary>
    IReadOnlyDictionary<string, string> GetConfig();

    /// <summary>Applies configuration from a flat dictionary.</summary>
    void SetConfig(IReadOnlyDictionary<string, string> config);
}
