namespace LocalTtsStudio.Core.Engines;

/// <summary>
/// Result of an environment check, as shown per engine on the diagnostics page. Each
/// check is reported separately so a failure points at the actual cause instead of a
/// single unhelpful "engine not working".
/// </summary>
public sealed record EngineEnvironmentReport
{
    public required string EngineId { get; init; }
    public required EngineStatus Status { get; init; }

    public CheckResult Runtime { get; init; } = CheckResult.Skipped;
    public CheckResult PythonInterpreter { get; init; } = CheckResult.Skipped;
    public CheckResult Imports { get; init; } = CheckResult.Skipped;
    public CheckResult Model { get; init; } = CheckResult.Skipped;
    public CheckResult Cuda { get; init; } = CheckResult.Skipped;

    public string? PythonVersion { get; init; }
    public string? TorchVersion { get; init; }
    public string? CudaVersion { get; init; }
    public string? DeviceName { get; init; }
    public long? AvailableVramBytes { get; init; }
    public string? EngineVersion { get; init; }

    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>Everything the "Copy diagnostic information" button puts on the clipboard.</summary>
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public enum CheckOutcome { Skipped, Ok, Warning, Failed }

public sealed record CheckResult(CheckOutcome Outcome, string? Message = null)
{
    public static CheckResult Skipped { get; } = new(CheckOutcome.Skipped);
    public static CheckResult Ok(string? message = null) => new(CheckOutcome.Ok, message);
    public static CheckResult Warning(string message) => new(CheckOutcome.Warning, message);
    public static CheckResult Failed(string message) => new(CheckOutcome.Failed, message);
}
