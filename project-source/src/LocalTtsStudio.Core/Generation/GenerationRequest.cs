using LocalTtsStudio.Core.Voices;

namespace LocalTtsStudio.Core.Generation;

/// <summary>
/// One unit of work: everything an engine needs to turn text into a WAV file.
/// </summary>
/// <remarks>
/// Deliberately engine-neutral. Engine-specific parameters live in
/// <see cref="Settings"/> as loose key/value pairs whose meaning is described by that
/// engine's <c>EngineSettingDefinition</c> list, so adding an engine with new parameters
/// requires no change to this type and therefore no change to the queue, the history or
/// the UI that builds requests.
/// </remarks>
public sealed record GenerationRequest
{
    public Guid RequestId { get; init; } = Guid.NewGuid();

    public required string EngineId { get; init; }
    public required string Text { get; init; }

    /// <summary>Canonical code. The adapter maps it; nothing upstream ever sees an engine code.</summary>
    public string? CanonicalLanguage { get; init; }

    public VoiceSelection Voice { get; init; } = VoiceSelection.None;

    /// <summary>
    /// Absolute paths to WAV reference recordings, already converted for this engine.
    /// Populated by the adapter from the voice character or from one-off input; the
    /// caller does not prepare these.
    /// </summary>
    public IReadOnlyList<string> ReferenceAudioPaths { get; init; } = Array.Empty<string>();

    public string? ReferenceTranscript { get; init; }

    /// <summary>Absolute path of the WAV to write. Already de-duplicated against existing files.</summary>
    public required string OutputFilePath { get; init; }

    /// <summary>
    /// Per-request scratch directory, handed to the worker so intermediate files land
    /// there instead of scattered through the engine's repository folder.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>Engine-specific parameters, keyed by EngineSettingDefinition.Id.</summary>
    public IReadOnlyDictionary<string, object?> Settings { get; init; } =
        new Dictionary<string, object?>();

    public T? GetSetting<T>(string id, T? fallback = default)
    {
        if (!Settings.TryGetValue(id, out var value) || value is null) return fallback;
        if (value is T typed) return typed;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
        {
            return fallback;
        }
    }
}
