namespace LocalTtsStudio.Core.Languages;

/// <summary>
/// One language the application knows about, plus how each engine spells it.
/// </summary>
/// <remarks>
/// The UI works only in canonical BCP-47-style codes ("en-GB", "no-NO") and shows only
/// human-readable names. Engines disagree wildly about language identifiers — Kokoro
/// uses single letters, XTTS uses two-letter codes, F5 often wants none at all — and
/// letting those leak into the interface would make the language picker unusable.
/// </remarks>
public sealed class LanguageDefinition
{
    /// <summary>Canonical code, e.g. "en-GB". The only form the UI and the database use.</summary>
    public required string CanonicalCode { get; init; }

    /// <summary>What the user sees, e.g. "English (UK)".</summary>
    public required string DisplayName { get; init; }

    /// <summary>Name in the language itself, e.g. "Norsk bokmål". Shown as a secondary line.</summary>
    public string? NativeName { get; init; }

    /// <summary>
    /// Per-engine code. A present key with a null value means "this engine supports the
    /// language but needs no code for it" — which is different from an absent key,
    /// meaning the engine cannot do this language at all. That distinction is the whole
    /// point of the mapping table.
    /// </summary>
    public Dictionary<string, string?> EngineMappings { get; init; } = new();

    public bool IsSupportedBy(string engineId) => EngineMappings.ContainsKey(engineId);

    /// <summary>
    /// The code to send to an engine. Throws when the engine does not support the
    /// language: silently substituting a different language produces audio in the wrong
    /// accent and no explanation, which is far worse than an error.
    /// </summary>
    public string? MapForEngine(string engineId)
    {
        if (!EngineMappings.TryGetValue(engineId, out var code))
        {
            throw new NotSupportedException(
                $"Engine '{engineId}' does not support {DisplayName} ({CanonicalCode}).");
        }

        return code;
    }
}
