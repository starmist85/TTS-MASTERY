using LocalTtsStudio.Core.Audio;

namespace LocalTtsStudio.Core.Abstractions;

/// <summary>
/// Converts a user's reference WAV into the exact shape an engine needs, and caches the
/// result.
/// </summary>
/// <remarks>
/// The original is never modified. Two copies exist deliberately: the user's reference,
/// which is theirs and must outlive any engine, and a derived engine-specific version
/// that is disposable. Caching is keyed on content hash plus the engine's requirements,
/// so the same recording is converted once per engine and reused until either the audio
/// or the requirements change.
/// </remarks>
public interface IAudioPreprocessingService
{
    /// <summary>
    /// Return a path to a WAV matching <paramref name="requirements"/>, converting and
    /// caching if needed. When the source already matches, the source path is returned
    /// unchanged rather than a pointless copy being made.
    /// </summary>
    Task<string> PrepareReferenceAsync(
        string sourceFilePath,
        ReferenceAudioRequirements requirements,
        CancellationToken cancellationToken = default);

    /// <summary>Read duration, sample rate, channels and size without decoding the whole file.</summary>
    Task<AudioFileInfo> ReadInfoAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy an imported WAV into a voice's storage under a stable name, so the voice no
    /// longer depends on wherever the user happened to have the file.
    /// </summary>
    Task<string> ImportReferenceAsync(
        string sourceFilePath,
        Guid voiceId,
        CancellationToken cancellationToken = default);

    /// <summary>SHA-256 of file contents, used as the cache key and to detect edits.</summary>
    Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Drop cached conversions derived from a reference whose audio changed.</summary>
    Task InvalidateCacheForAsync(string contentHash, CancellationToken cancellationToken = default);
}
