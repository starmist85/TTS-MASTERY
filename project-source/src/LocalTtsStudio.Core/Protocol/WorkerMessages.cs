using System.Text.Json.Serialization;

namespace LocalTtsStudio.Core.Protocol;

/// <summary>
/// The normalized request every Python worker understands, regardless of which engine it
/// wraps.
/// </summary>
/// <remarks>
/// <para>
/// One protocol, four engines. The alternative — teaching C# each project's command-line
/// syntax — means every upstream release can break the application, and it puts engine
/// knowledge in the wrong layer. Our own thin worker per engine absorbs that instead.
/// </para>
/// <para>
/// Transport is line-delimited JSON over stdin/stdout. Machine-readable messages go to
/// stdout and nothing else does; diagnostics, library chatter and progress bars from
/// third-party code go to stderr. Parsing console text as a protocol is how integrations
/// break silently on a library update.
/// </para>
/// </remarks>
public sealed record WorkerRequest
{
    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    /// <summary>"generate", "ping", "list_voices", "list_languages", "describe", "unload", "shutdown".</summary>
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    [JsonPropertyName("engine")]
    public string? Engine { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>Engine-specific language code, already mapped. Null when the engine needs none.</summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    /// <summary>Built-in voice id, when one was selected.</summary>
    [JsonPropertyName("voice")]
    public string? Voice { get; init; }

    /// <summary>Absolute paths to WAV references, already converted for this engine.</summary>
    [JsonPropertyName("referenceAudio")]
    public IReadOnlyList<string>? ReferenceAudio { get; init; }

    [JsonPropertyName("referenceTranscript")]
    public string? ReferenceTranscript { get; init; }

    [JsonPropertyName("outputFile")]
    public string? OutputFile { get; init; }

    /// <summary>Per-job scratch directory, so intermediates never land in the engine's repo folder.</summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; init; }

    [JsonPropertyName("settings")]
    public IReadOnlyDictionary<string, object?>? Settings { get; init; }
}

/// <summary>Message types a worker may emit on stdout.</summary>
public static class WorkerMessageTypes
{
    public const string Ready = "ready";
    public const string Status = "status";
    public const string Progress = "progress";
    public const string Completed = "completed";
    public const string Error = "error";
    public const string Voices = "voices";
    public const string Languages = "languages";
    public const string Info = "info";
}

/// <summary>
/// One line of worker output. A single shape covers every message type: the fields that
/// do not apply are simply absent, which keeps the worker side trivial to write in
/// Python and the C# side trivial to deserialise.
/// </summary>
public sealed record WorkerResponse
{
    [JsonPropertyName("requestId")]
    public string? RequestId { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Progress 0..1. Absent when the worker does not honestly know.</summary>
    [JsonPropertyName("value")]
    public double? Value { get; init; }

    [JsonPropertyName("outputFile")]
    public string? OutputFile { get; init; }

    [JsonPropertyName("durationSeconds")]
    public double? DurationSeconds { get; init; }

    /// <summary>Stable error code, e.g. "CUDA_OUT_OF_MEMORY", "MODEL_NOT_FOUND".</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("voices")]
    public IReadOnlyList<WorkerVoice>? Voices { get; init; }

    [JsonPropertyName("languages")]
    public IReadOnlyList<string>? Languages { get; init; }

    /// <summary>Free-form environment facts for the diagnostics page (python, torch, cuda, device).</summary>
    [JsonPropertyName("info")]
    public IReadOnlyDictionary<string, string>? Info { get; init; }
}

/// <summary>A built-in voice as the worker reports it, before canonical language mapping.</summary>
public sealed record WorkerVoice
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The engine's own language code. Mapped to canonical on the C# side.</summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    [JsonPropertyName("quality")]
    public string? Quality { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>Error codes shared by every worker, so C# can react without string matching on messages.</summary>
public static class WorkerErrorCodes
{
    public const string CudaOutOfMemory = "CUDA_OUT_OF_MEMORY";
    public const string ModelNotFound = "MODEL_NOT_FOUND";
    public const string ImportFailed = "IMPORT_FAILED";
    public const string InvalidReferenceAudio = "INVALID_REFERENCE_AUDIO";
    public const string UnsupportedLanguage = "UNSUPPORTED_LANGUAGE";
    public const string UnsupportedSetting = "UNSUPPORTED_SETTING";
    public const string InferenceFailed = "INFERENCE_FAILED";
    public const string WorkerCrashed = "WORKER_CRASHED";
    public const string Timeout = "TIMEOUT";
    public const string Cancelled = "CANCELLED";
}
