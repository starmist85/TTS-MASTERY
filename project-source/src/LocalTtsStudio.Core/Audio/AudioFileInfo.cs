namespace LocalTtsStudio.Core.Audio;

/// <summary>
/// Technical description of an audio file, shown next to each reference recording so the
/// user can see at a glance whether a take is usable — an 8 kHz mono phone recording and
/// a 48 kHz studio take look identical in a file list and clone very differently.
/// </summary>
public sealed record AudioFileInfo
{
    public required string FilePath { get; init; }
    public required AudioFormat Format { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int SampleRate { get; init; }
    public required int Channels { get; init; }
    public int BitsPerSample { get; init; }
    public long SizeBytes { get; init; }

    /// <summary>Peak amplitude 0..1 when it was cheap to compute. Null when not measured.</summary>
    public double? PeakAmplitude { get; init; }

    public string DurationDisplay => Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"m\:ss");

    public string SummaryDisplay =>
        $"{SampleRate / 1000.0:0.#} kHz · {(Channels == 1 ? "mono" : Channels == 2 ? "stereo" : $"{Channels} ch")} · {DurationDisplay}";
}
