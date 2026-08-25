namespace LocalTtsStudio.Core.Abstractions;

/// <summary>
/// Picks output filenames. Never overwrites: a generation the user has not listened to
/// yet is not a file the app gets to destroy.
/// </summary>
public interface IOutputFileNamingService
{
    /// <summary>
    /// First free filename in <paramref name="directory"/>. "narrator" becomes
    /// narrator.wav, then narrator_002.wav, then narrator_003.wav.
    /// </summary>
    string GetNextAvailableFilename(string directory, string baseName, string extension = ".wav");

    /// <summary>
    /// Strip characters Windows rejects, collapse whitespace, and refuse reserved device
    /// names (CON, PRN, NUL, COM1…). Returns a usable fallback rather than an empty string
    /// when the input sanitises down to nothing.
    /// </summary>
    string SanitizeFileName(string input, string fallback = "generation");

    /// <summary>Suggest a base name from the text being generated, for the filename field.</summary>
    string SuggestBaseName(string text, string fallback = "generation");
}
