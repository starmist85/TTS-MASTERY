namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// Canonical engine identifiers. These strings are persisted in the database, written
/// into engines.json and sent over the worker protocol, so they are part of the app's
/// data contract: changing one is a migration, not a rename.
/// </summary>
public static class EngineIds
{
    public const string F5 = "f5";
    public const string Kokoro = "kokoro";
    public const string Xtts = "xtts";
    public const string Fish = "fish";

    /// <summary>In-process engine used for UI development and tests. Never shipped enabled.</summary>
    public const string Mock = "mock";

    public static readonly IReadOnlyList<string> All = new[] { F5, Kokoro, Xtts, Fish };
}
