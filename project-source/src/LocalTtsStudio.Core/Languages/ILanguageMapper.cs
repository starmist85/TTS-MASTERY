namespace LocalTtsStudio.Core.Languages;

/// <summary>
/// The single place canonical language codes are translated into engine dialects.
/// </summary>
public interface ILanguageMapper
{
    /// <summary>Everything the app knows about, in display order.</summary>
    IReadOnlyList<LanguageDefinition> All { get; }

    LanguageDefinition? Find(string canonicalCode);

    /// <summary>Languages the given engine can actually produce, for the picker.</summary>
    IReadOnlyList<LanguageDefinition> ForEngine(string engineId);

    bool IsSupported(string canonicalCode, string engineId);

    /// <summary>
    /// Engine-specific code for a canonical language, or null when the engine needs none.
    /// Throws <see cref="NotSupportedException"/> when the engine cannot do it — never
    /// falls back to a different language.
    /// </summary>
    string? MapForEngine(string canonicalCode, string engineId);

    /// <summary>
    /// Reverse lookup, for turning an engine's own reported language list into canonical
    /// codes when enumerating built-in voices.
    /// </summary>
    LanguageDefinition? FromEngineCode(string engineCode, string engineId);
}
