namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// A voice that ships with an engine — Kokoro's voice packs, XTTS's studio speakers.
/// Enumerated from the installed engine at runtime, never hardcoded, because the set
/// depends on which voice files the user actually has on disk.
/// </summary>
/// <param name="Id">Identifier the engine expects, e.g. "af_heart". Sent verbatim to the worker.</param>
/// <param name="DisplayName">Human-readable name for the picker, e.g. "Heart (US female)".</param>
/// <param name="EngineId">Owning engine, so the picker can group by engine.</param>
/// <param name="CanonicalLanguage">Canonical language code, mapped from whatever the engine reports.</param>
/// <param name="Gender">Optional, for filtering. Null when the engine does not say.</param>
/// <param name="Quality">Optional engine-reported quality or grade.</param>
/// <param name="Tags">Free-form descriptors from the engine, for search.</param>
public sealed record BuiltInVoice(
    string Id,
    string DisplayName,
    string EngineId,
    string? CanonicalLanguage = null,
    string? Gender = null,
    string? Quality = null,
    IReadOnlyList<string>? Tags = null)
{
    public IReadOnlyList<string> Tags { get; init; } = Tags ?? Array.Empty<string>();
}
