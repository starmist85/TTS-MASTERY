using LocalTtsStudio.Core.Voices;

namespace LocalTtsStudio.Core.Abstractions;

/// <summary>
/// Persistence for voice characters and their references.
/// </summary>
/// <remarks>
/// Deleting a character removes its database rows and its folder on disk. That is the one
/// operation here that destroys user data, so it is explicit, confirmed in the UI, and
/// never triggered as a side effect of anything else.
/// </remarks>
public interface IVoiceCharacterRepository
{
    Task<IReadOnlyList<VoiceCharacter>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VoiceCharacter?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(VoiceCharacter character, CancellationToken cancellationToken = default);
    Task UpdateAsync(VoiceCharacter character, CancellationToken cancellationToken = default);

    /// <summary>Remove the character, its references, and its folder on disk.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default);

    /// <summary>Bump LastUsed and GenerationCount after a successful generation.</summary>
    Task RecordUsageAsync(Guid id, CancellationToken cancellationToken = default);
}
