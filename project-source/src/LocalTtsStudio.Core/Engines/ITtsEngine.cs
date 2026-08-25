using LocalTtsStudio.Core.Audio;
using LocalTtsStudio.Core.Generation;

namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// Everything the application knows about a TTS engine. The UI talks to this and nothing
/// else — no view, view-model or service contains F5-, Kokoro-, XTTS- or Fish-specific
/// code, which is what makes adding a fifth engine an adapter plus a worker rather than
/// a UI rewrite.
/// </summary>
public interface ITtsEngine : IAsyncDisposable
{
    string EngineId { get; }
    string DisplayName { get; }

    /// <summary>Engine files and runtime are present on this machine.</summary>
    bool IsInstalled { get; }

    /// <summary>Ready to accept a request right now.</summary>
    bool IsAvailable { get; }

    EngineStatus Status { get; }
    EngineCapabilities Capabilities { get; }

    /// <summary>Raised whenever <see cref="Status"/> changes, so the selector and status bar follow along.</summary>
    event EventHandler<EngineStatus>? StatusChanged;

    /// <summary>
    /// Reference audio shape this engine needs. The preprocessing service converts the
    /// user's WAV to match and caches the result.
    /// </summary>
    ReferenceAudioRequirements ReferenceRequirements { get; }

    /// <summary>
    /// Prepare the adapter. Cheap and safe to call at startup for every engine — it must
    /// not start a Python process or load a model, because doing that for four engines
    /// on launch would cost minutes and gigabytes of VRAM.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inspect runtime, interpreter, imports, model files and CUDA. Drives the
    /// diagnostics page and the first-run health check. Never throws for a broken
    /// environment — a broken environment is the answer, reported in the result.
    /// </summary>
    Task<EngineEnvironmentReport> CheckEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Voices shipping with this engine, enumerated from the installed copy. Empty when
    /// <c>Capabilities.SupportsBuiltInVoices</c> is false. Never a hardcoded list: the
    /// real set depends on which voice files are actually on disk.
    /// </summary>
    Task<IReadOnlyList<BuiltInVoice>> GetBuiltInVoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Canonical language codes this engine can produce.</summary>
    Task<IReadOnlyList<string>> GetAvailableLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Parameter metadata for this engine version, discovered from the installed
    /// repository rather than assumed. The UI renders editors from these.
    /// </summary>
    Task<IReadOnlyList<EngineSettingDefinition>> GetSettingDefinitionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check a request before it is queued: language supported, voice usable, references
    /// present when required, parameters in range. Catching this here means the user gets
    /// a specific message immediately instead of a worker failure two minutes later.
    /// </summary>
    Task<ValidationResult> ValidateRequestAsync(GenerationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate. Reports progress through <paramref name="progress"/> and returns a
    /// result — including for failure, which is a normal outcome rather than an
    /// exception, so the queue can record it and move on.
    /// </summary>
    Task<GenerationResult> GenerateAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Release the model but keep the adapter usable. Called by the model memory policy
    /// when switching engines on a machine that cannot hold two models at once.
    /// </summary>
    Task UnloadModelAsync(CancellationToken cancellationToken = default);

    /// <summary>Stop the worker and release everything. The adapter can be initialised again afterwards.</summary>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>Validation outcome. Warnings do not block generation; errors do.</summary>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    public static ValidationResult Valid { get; } =
        new(true, Array.Empty<string>(), Array.Empty<string>());

    public static ValidationResult Invalid(params string[] errors) =>
        new(false, errors, Array.Empty<string>());

    public static ValidationResult WithWarnings(params string[] warnings) =>
        new(true, Array.Empty<string>(), warnings);
}
