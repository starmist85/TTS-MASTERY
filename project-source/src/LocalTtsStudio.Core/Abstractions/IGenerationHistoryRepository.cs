using LocalTtsStudio.Core.History;

namespace LocalTtsStudio.Core.Abstractions;

public interface IGenerationHistoryRepository
{
    /// <summary>Newest first. Paged, because history grows without bound.</summary>
    Task<IReadOnlyList<GenerationHistoryEntry>> GetRecentAsync(
        int take = 200, int skip = 0, CancellationToken cancellationToken = default);

    Task<GenerationHistoryEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(GenerationHistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Remove the record. Does not touch the audio file — that is a separate, explicit action.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
