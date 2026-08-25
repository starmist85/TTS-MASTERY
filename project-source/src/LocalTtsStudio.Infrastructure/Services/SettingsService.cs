using System.Text.Json;
using LocalTtsStudio.Core.Abstractions;
using LocalTtsStudio.Core.Settings;
using Microsoft.Extensions.Logging;

namespace LocalTtsStudio.Infrastructure.Services;

/// <summary>
/// Settings persisted as JSON under the user data root.
/// </summary>
/// <remarks>
/// Not in the database, for two reasons: settings must load before the database is
/// opened (the data directory itself is a setting), and a settings file a user can open
/// in Notepad is one they can repair when something goes wrong. A corrupt file falls back
/// to defaults rather than blocking startup — an app that will not launch because of a bad
/// preference is an app with no way to fix the preference.
/// </remarks>
public sealed class SettingsService(IAppPaths paths, ILogger<SettingsService> logger) : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public AppSettings Current { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = paths.SettingsFilePath;

        if (!File.Exists(path))
        {
            logger.LogInformation("No settings file at {Path}; starting with defaults.", path);
            Current = new AppSettings();
            return;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            Current = await JsonSerializer
                          .DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                          .ConfigureAwait(false)
                      ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not read settings from {Path}; using defaults.", path);
            Current = new AppSettings();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = paths.SettingsFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Write to a temp file and move into place. A crash mid-write then leaves the
            // previous settings intact instead of a truncated file the app cannot parse.
            var tempPath = path + ".tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, Current, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not save settings.");
        }
        finally
        {
            _saveLock.Release();
        }

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        // Resets preferences only. Voices, history and generated audio are the user's
        // work and are never touched by a settings reset.
        Current = new AppSettings();
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }
}
