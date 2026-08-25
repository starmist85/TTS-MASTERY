using System.Globalization;
using System.Text;
using LocalTtsStudio.Core.Abstractions;

namespace LocalTtsStudio.Infrastructure.Services;

/// <summary>
/// Names output files, and never overwrites one.
/// </summary>
/// <remarks>
/// Generation is slow and the result may be the take the user wanted. Silently replacing
/// <c>narrator.wav</c> on the second run would destroy work that cannot be recovered, so
/// collisions always resolve to a new numbered file.
/// </remarks>
public sealed class OutputFileNamingService : IOutputFileNamingService
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public string GetNextAvailableFilename(string directory, string baseName, string extension = ".wav")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!extension.StartsWith('.')) extension = "." + extension;
        baseName = SanitizeFileName(baseName);

        var candidate = Path.Combine(directory, baseName + extension);
        if (!File.Exists(candidate)) return candidate;

        // _002 onwards, matching the first file's implicit _001. Three digits sorts
        // correctly in Explorer up to 999, which is well past where anyone renames things.
        for (var i = 2; i < 10_000; i++)
        {
            candidate = Path.Combine(directory, $"{baseName}_{i:D3}{extension}");
            if (!File.Exists(candidate)) return candidate;
        }

        // Absurd, but a timestamp is still better than throwing at the user.
        return Path.Combine(directory,
            $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
    }

    public string SanitizeFileName(string input, string fallback = "generation")
    {
        if (string.IsNullOrWhiteSpace(input)) return fallback;

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(input.Length);
        var lastWasSpace = false;

        foreach (var ch in input.Trim())
        {
            if (invalid.Contains(ch))
            {
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace) builder.Append(' ');
                lastWasSpace = true;
                continue;
            }

            builder.Append(ch);
            lastWasSpace = false;
        }

        // Windows silently strips trailing dots and spaces, which turns "report." into a
        // name that does not match what the user typed. Remove them explicitly instead.
        var result = builder.ToString().Trim().TrimEnd('.', ' ');

        if (result.Length == 0) return fallback;
        if (ReservedNames.Contains(result)) return result + "_file";
        if (result.Length > 120) result = result[..120].TrimEnd('.', ' ');

        return result.Length == 0 ? fallback : result;
    }

    public string SuggestBaseName(string text, string fallback = "generation")
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;

        // First handful of words, so the file is recognisable in Explorer without becoming
        // a sentence.
        var words = text
            .ReplaceLineEndings(" ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Any(char.IsLetterOrDigit))
            .Take(5)
            .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()))
            .Where(w => w.Length > 0)
            .ToArray();

        if (words.Length == 0) return fallback;

        var name = string.Join('_', words).ToLower(CultureInfo.InvariantCulture);
        return SanitizeFileName(name, fallback);
    }
}
