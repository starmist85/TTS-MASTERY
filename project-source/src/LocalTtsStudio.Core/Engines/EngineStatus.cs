namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// Lifecycle and health of an engine, as shown in the engine selector, the status bar
/// and the diagnostics page.
/// </summary>
public enum EngineState
{
    /// <summary>Not yet inspected. Transient — every engine leaves this on startup.</summary>
    Unknown = 0,

    /// <summary>Disabled in configuration. Not an error; the user turned it off.</summary>
    Disabled,

    /// <summary>Engine files or Python runtime are not present on this machine.</summary>
    NotInstalled,

    /// <summary>Installed but incompletely configured (paths missing or wrong).</summary>
    NotConfigured,

    /// <summary>Runtime is fine but the model weights are absent.</summary>
    MissingModel,

    /// <summary>Checking the environment right now.</summary>
    Checking,

    /// <summary>Worker process starting.</summary>
    Initializing,

    /// <summary>Worker alive, model loading into memory.</summary>
    LoadingModel,

    /// <summary>Ready to generate.</summary>
    Ready,

    /// <summary>Currently generating.</summary>
    Generating,

    /// <summary>Failed. <see cref="EngineStatus.Message"/> says how.</summary>
    Error
}

/// <summary>
/// A status plus the human-readable detail that goes beside it. The pair travels
/// together because a bare state ("Error") is never enough for the user to act on, and
/// a bare message cannot be styled.
/// </summary>
/// <param name="State">Machine-readable state.</param>
/// <param name="Message">One line, in the user's terms. Empty for uneventful states.</param>
/// <param name="Detail">Technical detail for the diagnostics page and the log.</param>
public sealed record EngineStatus(EngineState State, string Message = "", string? Detail = null)
{
    public bool IsUsable => State is EngineState.Ready or EngineState.Generating;

    /// <summary>A problem the user could plausibly fix (as opposed to a transient state).</summary>
    public bool IsFault => State is EngineState.Error
                                 or EngineState.NotInstalled
                                 or EngineState.NotConfigured
                                 or EngineState.MissingModel;

    public static EngineStatus Unknown { get; } = new(EngineState.Unknown);
    public static EngineStatus Ready { get; } = new(EngineState.Ready, "Ready");

    public static EngineStatus Error(string message, string? detail = null)
        => new(EngineState.Error, message, detail);
}
