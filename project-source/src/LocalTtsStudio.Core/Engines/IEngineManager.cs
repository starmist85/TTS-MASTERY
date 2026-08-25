using LocalTtsStudio.Core.Generation;

namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// Owns the set of engines: which exist, which are healthy, which model is currently
/// resident, and which engine a request goes to.
/// </summary>
/// <remarks>
/// One rule governs the whole type: a broken engine must never take down the others. If
/// Fish Speech cannot find its checkpoint, Kokoro still generates and the app still
/// starts. Every method here is written so a single engine's failure is captured and
/// reported rather than propagated.
/// </remarks>
public interface IEngineManager : IAsyncDisposable
{
    IReadOnlyList<ITtsEngine> Engines { get; }

    ITtsEngine? Find(string engineId);

    /// <summary>Raised when any engine's status changes.</summary>
    event EventHandler<EngineStatusChangedEventArgs>? EngineStatusChanged;

    /// <summary>
    /// Discover and initialise every configured engine. Failures are recorded as engine
    /// status, not thrown — startup completes even if every engine is broken, because a
    /// user with a broken install still needs to reach the diagnostics page.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Run environment checks across all engines, in parallel where safe.</summary>
    Task<IReadOnlyList<EngineEnvironmentReport>> CheckAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Every voice the app can offer: the user's characters first, then built-in voices
    /// grouped by engine. The picker renders this directly.
    /// </summary>
    Task<VoiceCatalog> GetVoiceCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Route a request to its engine, applying the model memory policy first (which may
    /// unload another engine's model to make room).
    /// </summary>
    Task<GenerationResult> GenerateAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task RestartEngineAsync(string engineId, CancellationToken cancellationToken = default);
    Task UnloadAllModelsAsync(CancellationToken cancellationToken = default);
}

public sealed class EngineStatusChangedEventArgs(string engineId, EngineStatus status) : EventArgs
{
    public string EngineId { get; } = engineId;
    public EngineStatus Status { get; } = status;
}
