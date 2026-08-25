using LocalTtsStudio.Core.Audio;

namespace LocalTtsStudio.Core.Voices;

/// <summary>
/// One reference recording belonging to a voice character.
/// </summary>
/// <remarks>
/// A character may hold several references even when the selected engine uses only one.
/// Storing them all is cheap and lossless; deciding how many actually reach inference is
/// the adapter's job, driven by <c>EngineCapabilities.MaxReferenceFiles</c>. That split
/// means adding an engine that supports multiple references later requires no data
/// migration.
/// </remarks>
public sealed class VoiceReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VoiceCharacterId { get; set; }

    /// <summary>Path relative to the voice's folder, e.g. "references/reference_001.wav".</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Filename the user imported, kept for display so the file stays recognisable.</summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Transcript for this specific recording. Falls back to the character's shared
    /// transcript when null, which is the common case.
    /// </summary>
    public string? Transcript { get; set; }

    public TimeSpan Duration { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>
    /// SHA-256 of the file. Drives the preprocessing cache and, when it changes, tells
    /// the app to invalidate every engine-specific conditioning cache derived from it.
    /// </summary>
    public string? ContentHash { get; set; }

    public DateTimeOffset AddedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Ordering in the editor, and the order references are passed to an engine.</summary>
    public int SortOrder { get; set; }

    public string SummaryDisplay =>
        $"{SampleRate / 1000.0:0.#} kHz · {(Channels == 1 ? "mono" : "stereo")} · " +
        (Duration.TotalHours >= 1 ? Duration.ToString(@"h\:mm\:ss") : Duration.ToString(@"m\:ss"));
}
