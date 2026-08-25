using System.Text.Json;
using LocalTtsStudio.Core.Abstractions;
using LocalTtsStudio.Core.Engines;
using Microsoft.Extensions.Logging;

namespace LocalTtsStudio.Infrastructure.Configuration;

/// <summary>
/// Reads engines.json and resolves every path in it through <see cref="IAppPaths"/>.
/// </summary>
/// <remarks>
/// Shipped configuration contains relative paths only. A packaged engines.json holding
/// <c>C:\Users\someone\source\repos\…</c> is the classic way an installer produces an app
/// that works on exactly one machine, so the loader resolves relative paths against the
/// installation root and never writes absolute developer paths back out.
/// </remarks>
public sealed class EngineConfigurationLoader(IAppPaths paths, ILogger<EngineConfigurationLoader> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public string ConfigurationFilePath => Path.Combine(paths.InstallationRoot, "engines.json");

    /// <summary>
    /// Load descriptors, falling back to built-in defaults when the file is absent. A
    /// missing engines.json is a first-run condition, not an error — the defaults describe
    /// the standard layout, and the diagnostics page will report anything actually missing
    /// on disk.
    /// </summary>
    public async Task<IReadOnlyList<EngineDescriptor>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var descriptors = await ReadFileAsync(cancellationToken).ConfigureAwait(false)
                          ?? CreateDefaults();

        foreach (var descriptor in descriptors)
        {
            descriptor.RepositoryPath = ResolveOrDefault(descriptor.RepositoryPath,
                Path.Combine("engines", descriptor.EngineId));

            descriptor.PythonExecutable = ResolveOrDefault(descriptor.PythonExecutable,
                Path.Combine("runtimes", descriptor.EngineId, "python.exe"));

            descriptor.WorkerScript = ResolveOrDefault(descriptor.WorkerScript,
                Path.Combine("workers", descriptor.EngineId, $"{descriptor.EngineId}_worker.py"));

            descriptor.ModelPath = ResolveOrDefault(descriptor.ModelPath,
                Path.Combine("models", descriptor.EngineId));
        }

        return descriptors;
    }

    private string ResolveOrDefault(string? configured, string fallback) =>
        paths.ResolveConfiguredPath(configured, fallback);

    private async Task<List<EngineDescriptor>?> ReadFileAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ConfigurationFilePath))
        {
            logger.LogInformation(
                "No engines.json at {Path}; using built-in defaults.", ConfigurationFilePath);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(ConfigurationFilePath);
            return await JsonSerializer
                .DeserializeAsync<List<EngineDescriptor>>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // A malformed config must not stop the app from starting — the user needs to
            // reach the settings screen to fix it.
            logger.LogError(ex, "Could not read {Path}; falling back to defaults.", ConfigurationFilePath);
            return null;
        }
    }

    /// <summary>
    /// The standard layout, matching what the installer stages. Engines start disabled
    /// until their environment check passes, so a fresh install shows honest status rather
    /// than four engines claiming to be ready.
    /// </summary>
    private static List<EngineDescriptor> CreateDefaults() =>
    [
        new() { EngineId = EngineIds.Kokoro, DisplayName = "Kokoro", Enabled = true },
        new() { EngineId = EngineIds.F5, DisplayName = "F5-TTS", Enabled = true },
        new() { EngineId = EngineIds.Xtts, DisplayName = "XTTS v2", Enabled = true },
        new() { EngineId = EngineIds.Fish, DisplayName = "Fish Speech", Enabled = true }
    ];

    public async Task SaveAsync(IReadOnlyList<EngineDescriptor> descriptors, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(ConfigurationFilePath);
        await JsonSerializer.SerializeAsync(stream, descriptors, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
