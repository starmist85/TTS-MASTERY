namespace LocalTtsStudio.Core.Abstractions;

public enum PlaybackState { Stopped = 0, Playing, Paused }

/// <summary>
/// In-app playback for reference recordings and generated output.
/// </summary>
/// <remarks>
/// Playback happens inside the application. Shelling out to the system's default player
/// for something the user will do dozens of times an hour is a poor experience — it steals
/// focus, opens a window per file, and makes A/B comparison impossible. "Open file
/// location" stays a separate, explicit action.
/// </remarks>
public interface IAudioPlayer : IDisposable
{
    PlaybackState State { get; }
    string? CurrentFilePath { get; }
    TimeSpan Position { get; set; }
    TimeSpan Duration { get; }

    /// <summary>0..1.</summary>
    double Volume { get; set; }

    event EventHandler<PlaybackState>? StateChanged;
    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler? PlaybackEnded;

    Task LoadAsync(string filePath, CancellationToken cancellationToken = default);
    void Play();
    void Pause();
    void Stop();

    /// <summary>Load and play in one call, for the play button on a list row.</summary>
    Task PlayFileAsync(string filePath, CancellationToken cancellationToken = default);
}
