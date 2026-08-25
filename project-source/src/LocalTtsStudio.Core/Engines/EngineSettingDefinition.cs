namespace LocalTtsStudio.Core.Engines;

public enum EngineSettingType
{
    Boolean,
    Integer,
    Double,
    Slider,
    String,
    Enum,
    File,
    Folder
}

/// <summary>
/// Metadata describing one engine parameter, so the UI can render an editor for it
/// without knowing what the parameter means.
/// </summary>
/// <remarks>
/// Adapters build these by inspecting the engine they wrap — the version actually
/// installed under tts-libraries, not what some release note says. An adapter that
/// invents a parameter the local engine does not accept produces a control that
/// silently does nothing, which is worse than not offering the control at all.
/// </remarks>
public sealed record EngineSettingDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required EngineSettingType Type { get; init; }

    /// <summary>Shown as a tooltip. Worth writing for anything a non-expert would not recognise.</summary>
    public string? Description { get; init; }

    public object? DefaultValue { get; init; }
    public double? Minimum { get; init; }
    public double? Maximum { get; init; }
    public double? Step { get; init; }

    /// <summary>Allowed values for <see cref="EngineSettingType.Enum"/>.</summary>
    public IReadOnlyList<EngineSettingChoice> Choices { get; init; } = Array.Empty<EngineSettingChoice>();

    /// <summary>
    /// Hidden behind the Advanced expander. Sampling internals belong here; speed and
    /// voice do not. A panel showing eighteen parameters at once reads as a config file.
    /// </summary>
    public bool IsAdvanced { get; init; }

    /// <summary>
    /// False when the installed engine version does not accept this parameter. The UI
    /// hides unsupported settings rather than showing them disabled, because a disabled
    /// control invites the user to go looking for how to enable it.
    /// </summary>
    public bool IsSupported { get; init; } = true;

    /// <summary>Format string for the numeric readout beside a slider, e.g. "0.00".</summary>
    public string? DisplayFormat { get; init; }

    /// <summary>Unit suffix for display, e.g. "x", "steps". Never part of the value.</summary>
    public string? Unit { get; init; }
}

public sealed record EngineSettingChoice(string Value, string DisplayName, string? Description = null);
