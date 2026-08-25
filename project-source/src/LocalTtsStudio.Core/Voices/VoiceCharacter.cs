namespace LocalTtsStudio.Core.Voices;

/// <summary>
/// A reusable speaker identity: reference recordings, their transcript, and the defaults
/// the user wants applied whenever they reach for this voice.
/// </summary>
/// <remarks>
/// A voice character is the user's data and outlives engines, app updates and model
/// changes. Audio lives on disk under the user data root; only metadata and relative
/// paths are stored in SQLite. Multi-gigabyte WAV blobs in a database make backups
/// miserable and gain nothing.
/// </remarks>
public sealed class VoiceCharacter
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedUtc { get; set; }

    /// <summary>Canonical language code, e.g. "en-GB". Never an engine-specific code.</summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>Engine pre-selected when this voice is chosen. Null means "leave the current engine".</summary>
    public string? PreferredEngineId { get; set; }

    /// <summary>
    /// Transcript of the reference audio, shared by all references unless a reference
    /// carries its own. Cloning engines that take a reference transcript are markedly
    /// better when it is accurate, so this stays editable forever.
    /// </summary>
    public string? ReferenceTranscript { get; set; }

    public List<VoiceReference> References { get; set; } = new();

    /// <summary>Path to an avatar image, relative to the voice's own folder.</summary>
    public string? AvatarPath { get; set; }

    public List<string> Tags { get; set; } = new();
    public string? Notes { get; set; }

    public bool IsFavorite { get; set; }
    public int GenerationCount { get; set; }

    /// <summary>
    /// Engine-specific metadata as a JSON blob, keyed by engine id. Lets an adapter
    /// remember something about this voice (a speaker embedding path, a conditioning
    /// cache id) without every engine's concerns leaking into this class.
    /// </summary>
    public Dictionary<string, string> EngineMetadata { get; set; } = new();

    public bool HasReferenceAudio => References.Count > 0;

    public string InitialsDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name)) return "?";
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}
