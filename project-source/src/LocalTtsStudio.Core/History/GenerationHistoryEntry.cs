using LocalTtsStudio.Core.Voices;

namespace LocalTtsStudio.Core.History;

/// <summary>
/// A record of one generation, kept so the user can replay, tweak or regenerate it later.
/// </summary>
/// <remarks>
/// The settings snapshot is the reason this exists. Being able to say "do that again but
/// slower" three days later requires knowing exactly what "that" was, and no amount of
/// remembering substitutes for storing the parameters at the moment they were used.
/// </remarks>
public sealed class GenerationHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    public string EngineId { get; set; } = string.Empty;
    public string EngineDisplayName { get; set; } = string.Empty;

    public VoiceSelectionKind VoiceKind { get; set; }
    public string? VoiceId { get; set; }
    public string? VoiceDisplayName { get; set; }

    /// <summary>Canonical code, so history stays readable if an engine changes its codes.</summary>
    public string? CanonicalLanguage { get; set; }

    public string InputText { get; set; } = string.Empty;

    public string? OutputFilePath { get; set; }
    public TimeSpan? AudioDuration { get; set; }
    public TimeSpan GenerationTime { get; set; }

    /// <summary>Serialised engine settings, exactly as used. Drives "duplicate settings".</summary>
    public string? SettingsJson { get; set; }

    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the output file is still on disk. Checked lazily rather than stored, since
    /// the user may delete or move files outside the app.
    /// </summary>
    public bool OutputExists => !string.IsNullOrWhiteSpace(OutputFilePath) && File.Exists(OutputFilePath);

    public string TextPreview => InputText.Length <= 90
        ? InputText.ReplaceLineEndings(" ")
        : InputText.ReplaceLineEndings(" ")[..90] + "…";
}
