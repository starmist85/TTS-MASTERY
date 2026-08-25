namespace LocalTtsStudio.Core.Audio;

/// <summary>
/// What one engine needs a reference recording to look like. The preprocessing service
/// converts an imported file to match, caches the result by content hash plus these
/// requirements, and hands the engine the converted copy.
/// </summary>
/// <remarks>
/// The original import is never modified. Two copies exist by design: the user's
/// reference, which is theirs and must survive engine changes, and a derived
/// engine-specific version that can be regenerated or discarded at any time.
/// </remarks>
public sealed record ReferenceAudioRequirements
{
    public required int SampleRate { get; init; }
    public int Channels { get; init; } = 1;
    public int BitsPerSample { get; init; } = 16;

    /// <summary>Bring peak amplitude to a consistent level. Helps engines sensitive to input gain.</summary>
    public bool Normalize { get; init; }

    /// <summary>Target peak in dBFS when <see cref="Normalize"/> is set. -1 leaves headroom.</summary>
    public double NormalizeTargetDbfs { get; init; } = -1.0;

    /// <summary>Trim leading and trailing silence. Off by default: trimming can remove breath that matters.</summary>
    public bool TrimSilence { get; init; }

    /// <summary>
    /// Cap the reference length. Most cloning engines use only the first several seconds
    /// and degrade with long input, so a cap is a quality setting, not only a size one.
    /// </summary>
    public TimeSpan? MaxDuration { get; init; }

    /// <summary>
    /// Stable key for the conversion cache. Any change in requirements yields a different
    /// key, which is how a file cached for an older engine version is never silently
    /// reused after the requirements change.
    /// </summary>
    public string CacheKey =>
        $"sr{SampleRate}_ch{Channels}_b{BitsPerSample}" +
        $"_n{(Normalize ? NormalizeTargetDbfs.ToString("0.#") : "0")}" +
        $"_t{(TrimSilence ? 1 : 0)}" +
        $"_d{(MaxDuration?.TotalSeconds.ToString("0") ?? "0")}";
}
