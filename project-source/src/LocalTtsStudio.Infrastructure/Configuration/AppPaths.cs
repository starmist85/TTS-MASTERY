using LocalTtsStudio.Core.Abstractions;

namespace LocalTtsStudio.Infrastructure.Configuration;

/// <summary>
/// The one implementation of <see cref="IAppPaths"/>. Everything derives from
/// <see cref="AppContext.BaseDirectory"/> and the user data root; nothing derives from the
/// current working directory, which changes depending on how the app was launched.
/// </summary>
public sealed class AppPaths : IAppPaths
{
    private readonly string? _userDataOverride;
    private readonly string? _modelRootOverride;

    /// <param name="userDataOverride">From settings. Null uses %LOCALAPPDATA%\LocalTtsStudio.</param>
    /// <param name="modelRootOverride">From settings. Null keeps models with the installation.</param>
    public AppPaths(string? userDataOverride = null, string? modelRootOverride = null)
    {
        _userDataOverride = string.IsNullOrWhiteSpace(userDataOverride) ? null : userDataOverride;
        _modelRootOverride = string.IsNullOrWhiteSpace(modelRootOverride) ? null : modelRootOverride;

        InstallationRoot = Path.GetFullPath(AppContext.BaseDirectory);
        IsDevelopmentMode = DetectDevelopmentMode(InstallationRoot, out var repoRoot);
        DevelopmentRepositoryRoot = repoRoot;
    }

    public bool IsDevelopmentMode { get; }

    /// <summary>Repository root when running from a source tree; null in a release.</summary>
    public string? DevelopmentRepositoryRoot { get; }

    public string InstallationRoot { get; }

    // In development, engine assets are read from the source tree so a developer can
    // point at freshly cloned repositories without staging a release first.
    public string RuntimeRoot => IsDevelopmentMode
        ? Path.Combine(DevelopmentRepositoryRoot!, "runtimes")
        : Path.Combine(InstallationRoot, "runtimes");

    public string WorkerRoot => IsDevelopmentMode
        ? Path.Combine(DevelopmentRepositoryRoot!, "project-source", "workers")
        : Path.Combine(InstallationRoot, "workers");

    public string EngineRoot => IsDevelopmentMode
        ? Path.Combine(DevelopmentRepositoryRoot!, "tts-libraries")
        : Path.Combine(InstallationRoot, "engines");

    public string ToolRoot => Path.Combine(InstallationRoot, "tools");

    public string UserDataRoot => _userDataOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LocalTtsStudio");

    public string DatabaseDirectory => Path.Combine(UserDataRoot, "database");
    public string DatabasePath => Path.Combine(DatabaseDirectory, "voices.db");
    public string VoicesDirectory => Path.Combine(UserDataRoot, "voices");
    public string GenerationsDirectory => Path.Combine(UserDataRoot, "generations");
    public string CacheDirectory => Path.Combine(UserDataRoot, "cache");
    public string JobsDirectory => Path.Combine(CacheDirectory, "jobs");
    public string LogsDirectory => Path.Combine(UserDataRoot, "logs");
    public string SettingsFilePath => Path.Combine(UserDataRoot, "settings", "settings.json");

    public string ModelRoot => _modelRootOverride ?? (IsDevelopmentMode
        ? Path.Combine(DevelopmentRepositoryRoot!, "models")
        : Path.Combine(InstallationRoot, "models"));

    public string GetVoiceDirectory(Guid voiceId) =>
        Path.Combine(VoicesDirectory, voiceId.ToString("D"));

    public string GetVoiceReferencesDirectory(Guid voiceId) =>
        Path.Combine(GetVoiceDirectory(voiceId), "references");

    public string GetVoiceCacheDirectory(Guid voiceId, string engineId) =>
        Path.Combine(GetVoiceDirectory(voiceId), "cache", engineId);

    public string GetJobDirectory(Guid requestId) =>
        Path.Combine(JobsDirectory, requestId.ToString("D"));

    public string GetEngineDirectory(string engineId) => Path.Combine(EngineRoot, engineId);
    public string GetEngineRuntimeDirectory(string engineId) => Path.Combine(RuntimeRoot, engineId);
    public string GetEngineWorkerDirectory(string engineId) => Path.Combine(WorkerRoot, engineId);

    public string FfmpegPath => Path.Combine(ToolRoot, "ffmpeg", "ffmpeg.exe");
    public string FfprobePath => Path.Combine(ToolRoot, "ffmpeg", "ffprobe.exe");

    public void EnsureUserDirectoriesExist()
    {
        foreach (var dir in new[]
                 {
                     UserDataRoot, DatabaseDirectory, VoicesDirectory, GenerationsDirectory,
                     CacheDirectory, JobsDirectory, LogsDirectory,
                     Path.GetDirectoryName(SettingsFilePath)!
                 })
        {
            Directory.CreateDirectory(dir);
        }
    }

    public string ResolveConfiguredPath(string? configuredPath, string fallbackRelativeToInstall)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(Path.Combine(InstallationRoot, fallbackRelativeToInstall));

        // An absolute configured path wins; a relative one is relative to the install
        // root, never to the working directory. That is what lets one engines.json file
        // be valid both in the dev tree and under Program Files.
        return Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(InstallationRoot, configuredPath));
    }

    /// <summary>
    /// Development mode is detected by walking up from the binary looking for the
    /// repository's marker files. A release install has no such ancestor, so this is
    /// false there without needing a build flag that someone will eventually ship wrong.
    /// </summary>
    private static bool DetectDevelopmentMode(string startDirectory, out string? repositoryRoot)
    {
        var dir = new DirectoryInfo(startDirectory);

        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            var hasSolution = File.Exists(Path.Combine(dir.FullName, "project-source", "LocalTtsStudio.sln"));
            var hasLibraries = Directory.Exists(Path.Combine(dir.FullName, "tts-libraries"));

            if (hasSolution || hasLibraries)
            {
                repositoryRoot = dir.FullName;
                return true;
            }
        }

        repositoryRoot = null;
        return false;
    }
}
