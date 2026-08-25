namespace LocalTtsStudio.Core.Abstractions;

/// <summary>
/// Brings the database up to date at startup.
/// </summary>
/// <remarks>
/// The rule this exists to enforce: an application update never destroys or resets the
/// user's database. Voice characters and history are the user's work, often representing
/// hours of recording and transcription, and a schema change is never a licence to drop
/// a table.
/// </remarks>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
