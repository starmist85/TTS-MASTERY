using LocalTtsStudio.Core.Engines;

namespace LocalTtsStudio.Core.Voices;

public enum VoiceSelectionKind
{
    /// <summary>No saved voice. The engine speaks in its default voice, or uses one-off reference audio.</summary>
    None = 0,

    /// <summary>A voice character from the user's library.</summary>
    Character,

    /// <summary>A voice that ships with the engine.</summary>
    BuiltIn
}

/// <summary>
/// Whatever the user picked in the voice selector, in one shape.
/// </summary>
/// <remarks>
/// A saved character and a built-in engine voice are both "a voice you can generate
/// with" as far as the UI is concerned. Modelling them as one selection type is what
/// keeps the generation screen from branching on which kind it got — the branch belongs
/// in the adapter, where the difference actually matters, and nowhere else.
/// </remarks>
public sealed record VoiceSelection
{
    public VoiceSelectionKind Kind { get; init; } = VoiceSelectionKind.None;

    /// <summary>Character id, or built-in voice id, or empty for <see cref="VoiceSelectionKind.None"/>.</summary>
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = "No voice";

    /// <summary>Engine that owns a built-in voice. Null for characters, which are engine-agnostic.</summary>
    public string? OwningEngineId { get; init; }

    /// <summary>Canonical language associated with the voice, when it has one.</summary>
    public string? CanonicalLanguage { get; init; }

    public static VoiceSelection None { get; } = new();

    public static VoiceSelection FromCharacter(VoiceCharacter character) => new()
    {
        Kind = VoiceSelectionKind.Character,
        Id = character.Id.ToString(),
        DisplayName = character.Name,
        OwningEngineId = null,
        CanonicalLanguage = character.DefaultLanguage
    };

    public static VoiceSelection FromBuiltIn(BuiltInVoice voice) => new()
    {
        Kind = VoiceSelectionKind.BuiltIn,
        Id = voice.Id,
        DisplayName = voice.DisplayName,
        OwningEngineId = voice.EngineId,
        CanonicalLanguage = voice.CanonicalLanguage
    };

    /// <summary>
    /// A built-in voice only works on the engine that owns it; a character works
    /// anywhere. Used to grey out voices that the currently selected engine cannot use,
    /// rather than letting the user pick one and fail at generation time.
    /// </summary>
    public bool IsUsableWith(string engineId) =>
        Kind != VoiceSelectionKind.BuiltIn
        || string.Equals(OwningEngineId, engineId, StringComparison.OrdinalIgnoreCase);
}
