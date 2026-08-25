namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// Configuration for one engine, loaded from engines.json. Paths here are resolved
/// through IAppPaths and may be relative to the installation root, which is what keeps
/// the same configuration file valid in the dev tree and under Program Files.
/// </summary>
public sealed class EngineDescriptor
{
    public required string EngineId { get; set; }
    public required string DisplayName { get; set; }

    /// <summary>Engine package or repository root, relative to the engine root or absolute.</summary>
    public string? RepositoryPath { get; set; }

    /// <summary>python.exe for this engine's isolated runtime.</summary>
    public string? PythonExecutable { get; set; }

    /// <summary>Our worker script, not the engine's own CLI.</summary>
    public string? WorkerScript { get; set; }

    /// <summary>Directory holding this engine's model weights.</summary>
    public string? ModelPath { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Bumped when the runtime build scripts change in a way that invalidates an
    /// existing runtime, so the app can tell the user to rebuild rather than failing
    /// with an obscure import error.
    /// </summary>
    public string? EnvironmentVersion { get; set; }

    /// <summary>Extra environment variables for the worker process (HF_HUB_OFFLINE and friends).</summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>Seconds to wait for the worker to report ready before declaring failure.</summary>
    public int StartupTimeoutSeconds { get; set; } = 180;
}
