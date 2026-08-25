namespace LocalTtsStudio.Core.Generation;

/// <summary>
/// Where a job is in its life. Reported to the queue and the status bar.
/// </summary>
public enum GenerationStage
{
    Pending = 0,
    Preparing,
    LoadingEngine,
    LoadingModel,
    Generating,
    Saving,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// A progress report from an engine.
/// </summary>
/// <remarks>
/// <see cref="Message"/> matters more than <see cref="Value"/>. Most TTS work has no
/// honest percentage — the model either loads or it does not, and generation is a single
/// forward pass — so a named stage ("Loading model, first run is slower") communicates
/// far more than an invented number. Leave <see cref="Value"/> null rather than guess;
/// the UI shows an indeterminate bar with the stage text, which is truthful.
/// </remarks>
public sealed record GenerationProgress
{
    public required Guid RequestId { get; init; }
    public required GenerationStage Stage { get; init; }

    /// <summary>0..1 when genuinely known, null otherwise.</summary>
    public double? Value { get; init; }

    /// <summary>Short stage description shown under the progress bar.</summary>
    public string? Message { get; init; }

    public static GenerationProgress At(Guid id, GenerationStage stage, string? message = null, double? value = null)
        => new() { RequestId = id, Stage = stage, Message = message, Value = value };
}
