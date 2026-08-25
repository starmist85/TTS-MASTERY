using LocalTtsStudio.Core.Abstractions;
using LocalTtsStudio.Core.History;
using Microsoft.EntityFrameworkCore;

namespace LocalTtsStudio.Infrastructure.Persistence;

public sealed class GenerationHistoryRepository(IDbContextFactory<AppDbContext> contextFactory)
    : IGenerationHistoryRepository
{
    public async Task<IReadOnlyList<GenerationHistoryEntry>> GetRecentAsync(
        int take = 200, int skip = 0, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.GenerationHistory
            .AsNoTracking()
            .OrderByDescending(h => h.TimestampUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GenerationHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.GenerationHistory
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(GenerationHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.GenerationHistory.Add(entry);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Deleting the record leaves the audio file alone. Removing a history row and the
        // user's generated audio are different intentions, and the UI asks about them
        // separately.
        await context.GenerationHistory
            .Where(h => h.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await context.GenerationHistory.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.GenerationHistory.CountAsync(cancellationToken).ConfigureAwait(false);
    }
}
