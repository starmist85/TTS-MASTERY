namespace LocalTtsStudio.Core.Abstractions;

/// <summary>
/// Every path the application uses, resolved in one place.
/// </summary>
/// <remarks>
/// <para>
/// The single question every path decision is measured against: <em>will this still work
/// after being installed under Program Files on a different Windows computer?</em>
/// Scattering path strings through the codebase is how an app ends up working only on the
/// machine it was written on.
/// </para>
/// <para>
/// Two modes. In development, engine paths point into the source tree's tts-libraries
/// folder. In a release, they point at packaged directories under the installation root.
/// Nothing outside this service knows which mode it is in, and nothing anywhere relies on
/// the current working directory — paths derive from
/// <see cref="AppContext.BaseDirectory"/>.
/// </para>
/// </remarks>
public interface IAppPaths
{
    /// <summary>True when running from a source tree rather than an installed package.</summary>
    bool IsDevelopmentMode { get; }

    // ── Installation (read-only at runtime) ───────────────────────────────────
    string InstallationRoot { get; }
    string RuntimeRoot { get; }
    string WorkerRoot { get; }
    string EngineRoot { get; }
    string ToolRoot { get; }

    // ── User data (writable; survives uninstall) ──────────────────────────────
    string UserDataRoot { get; }
    string DatabaseDirectory { get; }
    string DatabasePath { get; }
    string VoicesDirectory { get; }
    string GenerationsDirectory { get; }
    string CacheDirectory { get; }
    string JobsDirectory { get; }
    string LogsDirectory { get; }
    string SettingsFilePath { get; }

    /// <summary>Model weights. Redirectable to another drive; never assumed to be inside Program Files.</summary>
    string ModelRoot { get; }

    // ── Resolution helpers ────────────────────────────────────────────────────
    string GetVoiceDirectory(Guid voiceId);
    string GetVoiceReferencesDirectory(Guid voiceId);
    string GetVoiceCacheDirectory(Guid voiceId, string engineId);
    string GetJobDirectory(Guid requestId);
    string GetEngineDirectory(string engineId);
    string GetEngineRuntimeDirectory(string engineId);
    string GetEngineWorkerDirectory(string engineId);

    /// <summary>bundled ffmpeg.exe. Never resolved from PATH — a global FFmpeg may be any version.</summary>
    string FfmpegPath { get; }
    string FfprobePath { get; }

    /// <summary>Create every user data directory. Called once at startup.</summary>
    void EnsureUserDirectoriesExist();

    /// <summary>Turn a possibly-relative configured path into an absolute one under the right root.</summary>
    string ResolveConfiguredPath(string? configuredPath, string fallbackRelativeToInstall);
}
