using LocalTtsStudio.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalTtsStudio.Infrastructure.Persistence;

/// <summary>
/// Creates or updates the database at startup, without ever destroying what is there.
/// </summary>
/// <remarks>
/// <para>
/// Until the schema settles this uses <c>EnsureCreatedAsync</c>, which creates the
/// database on first run and does nothing when it already exists. That is safe — it never
/// drops anything — but it also cannot evolve a schema.
/// </para>
/// <para>
/// Before the first release that ships to another machine, switch to EF migrations:
/// generate the initial migration with <c>dotnet ef migrations add Initial</c> from the
/// Infrastructure project, then replace the EnsureCreated call below with
/// <c>MigrateAsync</c>. Shipping EnsureCreated and later adding a column is how user
/// databases get recreated from scratch, and a voice library represents hours of the
/// user's recording and transcription work.
/// </para>
/// </remarks>
public sealed class DatabaseInitializer(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppPaths paths,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.DatabaseDirectory);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var created = await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            created ? "Created database at {Path}." : "Opened existing database at {Path}.",
            paths.DatabasePath);
    }
}
