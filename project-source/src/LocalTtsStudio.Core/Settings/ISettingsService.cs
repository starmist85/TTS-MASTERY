namespace LocalTtsStudio.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/>. A single mutable instance is shared through
/// DI, so a change made on the settings screen is visible everywhere immediately without
/// a change-notification web between services.
/// </summary>
public interface ISettingsService
{
    AppSettings Current { get; }

    /// <summary>
    /// Raised after a save. Services that cache a derived value (log level, theme) listen
    /// for this rather than polling.
    /// </summary>
    event EventHandler? SettingsChanged;

    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>Restore defaults without touching voices, history or generated audio.</summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
