using LocalTtsStudio.Core.Voices;

namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// Everything selectable in the voice picker, in the order it should appear.
/// </summary>
/// <remarks>
/// The user's own voices come first and stand alone. They are the ones the user made,
/// named and cares about; burying them under a long alphabetical list of engine-supplied
/// voices makes the library feel like someone else's. Built-in voices follow, grouped by
/// engine so it is obvious which ones the current engine can actually use.
/// </remarks>
public sealed record VoiceCatalog
{
    /// <summary>The user's saved voice characters. Always listed first, in their own section.</summary>
    public IReadOnlyList<VoiceCharacter> UserVoices { get; init; } = Array.Empty<VoiceCharacter>();

    /// <summary>Voices shipped by engines, grouped by engine and enumerated from disk.</summary>
    public IReadOnlyList<BuiltInVoiceGroup> BuiltInGroups { get; init; } = Array.Empty<BuiltInVoiceGroup>();

    public int TotalCount => UserVoices.Count + BuiltInGroups.Sum(g => g.Voices.Count);

    public static VoiceCatalog Empty { get; } = new();
}

/// <param name="EngineId">Engine that owns these voices.</param>
/// <param name="EngineDisplayName">Section header in the picker.</param>
/// <param name="Voices">Voices, in the engine's own order unless it gives none.</param>
public sealed record BuiltInVoiceGroup(
    string EngineId,
    string EngineDisplayName,
    IReadOnlyList<BuiltInVoice> Voices);
