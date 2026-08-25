namespace LocalTtsStudio.Core.Generation;

/// <summary>
/// Outcome of a generation. Failure is a normal result, not an exception: an engine that
/// runs out of VRAM or cannot find a checkpoint is reporting a fact about the machine,
/// and the queue needs to record it, show it and carry on with the next job.
/// </summary>
public sealed record GenerationResult
{
    public required Guid RequestId { get; init; }
    public required bool Success { get; init; }

    public string? OutputFilePath { get; init; }
    public TimeSpan? AudioDuration { get; init; }
    public TimeSpan GenerationTime { get; init; }

    /// <summary>Stable error code for programmatic handling, e.g. "CUDA_OUT_OF_MEMORY".</summary>
    public string? ErrorCode { get; init; }

    /// <summary>One line, in the user's terms.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Stack trace, stderr tail, worker output. For the log and the diagnostics copy button.</summary>
    public string? ErrorDetail { get; init; }

    public bool WasCancelled { get; init; }

    public static GenerationResult Ok(Guid requestId, string outputPath, TimeSpan generationTime, TimeSpan? audioDuration = null)
        => new()
        {
            RequestId = requestId,
            Success = true,
            OutputFilePath = outputPath,
            GenerationTime = generationTime,
            AudioDuration = audioDuration
        };

    public static GenerationResult Failed(Guid requestId, string code, string message, string? detail = null)
        => new()
        {
            RequestId = requestId,
            Success = false,
            ErrorCode = code,
            ErrorMessage = message,
            ErrorDetail = detail
        };

    public static GenerationResult Cancelled(Guid requestId)
        => new() { RequestId = requestId, Success = false, WasCancelled = true, ErrorMessage = "Cancelled" };
}
