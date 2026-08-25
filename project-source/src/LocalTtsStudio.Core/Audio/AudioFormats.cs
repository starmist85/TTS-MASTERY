namespace LocalTtsStudio.Core.Audio;

/// <summary>
/// Container/codec of an audio file. The application is WAV-only end to end; the other
/// members exist so an unsupported file can be recognised and reported precisely rather
/// than failing as "could not read file".
/// </summary>
public enum AudioFormat
{
    Unknown = 0,
    Wav,
    Mp3,
    Flac,
    Ogg,
    M4a
}

/// <summary>
/// One audio format, everywhere: PCM WAV on import, at the engine boundary, and on
/// output.
/// </summary>
/// <remarks>
/// <para>
/// Uniform WAV removes a whole category of failure. No decoder dependency at import, no
/// encoder dependency at save, no lossy round-trip between the reference the user hears
/// and the audio the engine receives, and no adapter that has to think about container
/// formats. Every engine is handed PCM WAV at the sample rate it asks for, and every
/// generation is written as PCM WAV — which is what the engines emit anyway.
/// </para>
/// <para>
/// A non-WAV file is rejected at import with a message naming the format, not swallowed
/// and not silently converted. If MP3/FLAC support is ever wanted, it belongs here as an
/// explicit widening of <see cref="ImportExtensions"/> plus a decode step in the audio
/// preprocessing service — not as an ad-hoc conversion inside an engine adapter.
/// </para>
/// </remarks>
public static class AudioFormats
{
    /// <summary>The only extension accepted for reference audio import.</summary>
    public static readonly IReadOnlyList<string> ImportExtensions = new[] { ".wav" };

    /// <summary>The only extension generated audio is written with.</summary>
    public const string MasterExtension = ".wav";

    public const AudioFormat Master = AudioFormat.Wav;

    public static string BuildFileDialogFilter() => "WAV audio (*.wav)|*.wav";

    public static AudioFormat FromExtension(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return AudioFormat.Unknown;

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".wav" => AudioFormat.Wav,
            ".mp3" => AudioFormat.Mp3,
            ".flac" => AudioFormat.Flac,
            ".ogg" => AudioFormat.Ogg,
            ".m4a" or ".aac" => AudioFormat.M4a,
            _ => AudioFormat.Unknown
        };
    }

    public static string ToExtension(AudioFormat format) => format switch
    {
        AudioFormat.Wav => ".wav",
        AudioFormat.Mp3 => ".mp3",
        AudioFormat.Flac => ".flac",
        AudioFormat.Ogg => ".ogg",
        AudioFormat.M4a => ".m4a",
        _ => ".wav"
    };

    public static bool IsSupportedForImport(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && string.Equals(Path.GetExtension(path), MasterExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Message shown when the user drops or browses to something that is not a WAV. It
    /// names the format they actually gave us, because "unsupported file" tells them
    /// nothing they can act on.
    /// </summary>
    public static string DescribeRejection(string path)
    {
        var format = FromExtension(path);
        var name = format == AudioFormat.Unknown
            ? Path.GetExtension(path) is { Length: > 0 } ext ? ext.TrimStart('.').ToUpperInvariant() : "this file type"
            : format.ToString().ToUpperInvariant();

        return $"{name} files are not supported. Local TTS Studio works with WAV audio only — " +
               "convert the file to WAV and import it again.";
    }
}
