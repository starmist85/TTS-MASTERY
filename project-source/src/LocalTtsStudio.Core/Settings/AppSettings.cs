namespace LocalTtsStudio.Core.Settings;

/// <summary>
/// How aggressively models are kept resident in VRAM.
/// </summary>
/// <remarks>
/// Loading a multi-gigabyte TTS model takes seconds to minutes, so keeping one resident
/// is the difference between a tool that feels instant and one that feels like a batch
/// job. But four resident models will exhaust any consumer GPU, so this is a real
/// trade-off the user should own rather than one the app guesses at.
/// </remarks>
public enum ModelMemoryPolicy
{
    /// <summary>Unload the previous model whenever the engine changes. Slowest, safest.</summary>
    Conservative = 0,

    /// <summary>Keep the most recently used model if there is VRAM headroom. The default.</summary>
    Balanced,

    /// <summary>Keep every used model loaded. Fastest, and the first to hit an out-of-memory error.</summary>
    Performance
}

public enum DevicePreference
{
    /// <summary>Use CUDA when available, fall back to CPU.</summary>
    Auto = 0,
    ForceGpu,
    ForceCpu
}

public enum AppTheme
{
    Dark = 0,
    Light,

    /// <summary>Follow the Windows app theme, and follow changes to it while running.</summary>
    System
}

/// <summary>
/// User-facing application settings. Persisted as JSON under the user data root, not in
/// the database — settings are small, need to be readable and repairable by hand when
/// something goes wrong, and must load before the database is opened.
/// </summary>
public sealed class AppSettings
{
    // ── General ───────────────────────────────────────────────────────────────
    public string? DefaultEngineId { get; set; }
    public string DefaultLanguage { get; set; } = "en-US";
    public bool RememberLastVoice { get; set; } = true;
    public string? LastVoiceId { get; set; }
    public bool AutoPlayAfterGeneration { get; set; } = true;

    // ── Output ────────────────────────────────────────────────────────────────
    /// <summary>Null means Documents\Local TTS Studio\Generated, resolved at first use.</summary>
    public string? OutputDirectory { get; set; }
    public string DefaultFileNameBase { get; set; } = "generation";

    // ── Performance ───────────────────────────────────────────────────────────
    public ModelMemoryPolicy ModelMemoryPolicy { get; set; } = ModelMemoryPolicy.Balanced;
    public DevicePreference DevicePreference { get; set; } = DevicePreference.Auto;

    /// <summary>
    /// Concurrent GPU generations. One, until proven otherwise: two jobs sharing a GPU
    /// usually finish later than the same two run in sequence, and are far more likely to
    /// exhaust VRAM.
    /// </summary>
    public int MaxConcurrentGenerations { get; set; } = 1;

    // ── Storage ───────────────────────────────────────────────────────────────
    /// <summary>Null means %LOCALAPPDATA%\LocalTtsStudio.</summary>
    public string? UserDataDirectory { get; set; }

    /// <summary>Null means models live with the installation. Lets weights go on a bigger drive.</summary>
    public string? ModelRootDirectory { get; set; }

    public bool DeleteTemporaryFilesAutomatically { get; set; } = true;
    public int JobCacheRetentionDays { get; set; } = 7;

    // ── Appearance ────────────────────────────────────────────────────────────
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    // ── Diagnostics ───────────────────────────────────────────────────────────
    /// <summary>Serilog level name: Verbose, Debug, Information, Warning, Error, Fatal.</summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Log the text being synthesised. Off by default — generation text is the user's
    /// content and there is no reason for it to sit in a log file by default.
    /// </summary>
    public bool LogGenerationText { get; set; }

    // ── Window ────────────────────────────────────────────────────────────────
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool WindowMaximized { get; set; }

    /// <summary>Splitter positions on the generation screen, so a layout the user set stays set.</summary>
    public double? GenerateLeftPaneWidth { get; set; }
    public double? GenerateRightPaneWidth { get; set; }
}
