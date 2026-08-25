namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// What an engine can actually do. The UI builds itself from this rather than from a
/// chain of <c>if (engineId == "f5")</c> checks, which is what lets a new engine be
/// added without touching a single view.
/// </summary>
/// <remarks>
/// Capabilities describe the *shape* of an engine's input — whether it takes reference
/// audio at all, whether a language code is meaningful. Numeric ranges and defaults for
/// individual parameters live in <see cref="EngineSettingDefinition"/> instead, because
/// those vary per engine version and are better discovered from the installed
/// repository than hardcoded here.
/// </remarks>
public sealed record EngineCapabilities
{
    // ── Voice input ───────────────────────────────────────────────────────────
    public bool SupportsReferenceAudio { get; init; }
    public bool SupportsReferenceTranscript { get; init; }
    public bool SupportsMultipleReferences { get; init; }
    public bool SupportsVoiceCloning { get; init; }

    /// <summary>
    /// The engine ships its own voices (Kokoro's voice packs, XTTS's studio speakers).
    /// When true, <see cref="ITtsEngine.GetBuiltInVoicesAsync"/> returns a real list and
    /// the voice picker shows a "Built-in voices" section for this engine.
    /// </summary>
    public bool SupportsBuiltInVoices { get; init; }

    // ── Language ──────────────────────────────────────────────────────────────
    /// <summary>
    /// The engine takes a language code. Some engines infer language from the text or
    /// the reference audio and take no code at all — for those this is false and the
    /// language selector is hidden rather than shown with a meaningless value.
    /// </summary>
    public bool SupportsLanguageSelection { get; init; }

    // ── Generation parameters ─────────────────────────────────────────────────
    public bool SupportsSpeed { get; init; }
    public bool SupportsPitch { get; init; }
    public bool SupportsTemperature { get; init; }
    public bool SupportsTopP { get; init; }
    public bool SupportsTopK { get; init; }
    public bool SupportsSeed { get; init; }
    public bool SupportsCfg { get; init; }
    public bool SupportsEmotion { get; init; }
    public bool SupportsStyle { get; init; }
    public bool SupportsModelSelection { get; init; }

    // ── Execution ─────────────────────────────────────────────────────────────
    public bool SupportsStreaming { get; init; }
    public bool SupportsCancellation { get; init; }
    public bool SupportsCpuFallback { get; init; }
    public bool SupportsTraining { get; init; }
    public bool SupportsFineTuning { get; init; }

    /// <summary>
    /// Maximum reference recordings the adapter will pass to the engine. The Voice
    /// Character model always stores many; this is how many actually reach inference.
    /// </summary>
    public int MaxReferenceFiles { get; init; } = 1;

    /// <summary>Sample rate the engine emits, for display and for output metadata.</summary>
    public int OutputSampleRate { get; init; } = 24000;

    /// <summary>
    /// A voice source is required (the engine cannot speak without either a built-in
    /// voice or reference audio). Drives validation before a request is queued.
    /// </summary>
    public bool RequiresVoiceSource { get; init; } = true;

    public static EngineCapabilities None { get; } = new();
}
