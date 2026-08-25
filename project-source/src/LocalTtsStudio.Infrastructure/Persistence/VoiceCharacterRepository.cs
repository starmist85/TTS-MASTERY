using LocalTtsStudio.Core.Abstractions;
using LocalTtsStudio.Core.Voices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalTtsStudio.Infrastructure.Persistence;

public sealed class VoiceCharacterRepository(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppPaths paths,
    ILogger<VoiceCharacterRepository> logger) : IVoiceCharacterRepository
{
    public async Task<IReadOnlyList<VoiceCharacter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // AsNoTracking: the library screen reads, it does not edit. Tracking hundreds of
        // graphs for a read-only list is pure overhead.
        return await context.VoiceCharacters
            .AsNoTracking()
            .Include(v => v.References)
            .OrderByDescending(v => v.IsFavorite)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<VoiceCharacter?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.VoiceCharacters
            .AsNoTracking()
            .Include(v => v.References.OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(VoiceCharacter character, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.GetVoiceReferencesDirectory(character.Id));

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        character.CreatedUtc = DateTimeOffset.UtcNow;
        character.ModifiedUtc = character.CreatedUtc;

        context.VoiceCharacters.Add(character);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Created voice character {Name} ({Id}).", character.Name, character.Id);
    }

    public async Task UpdateAsync(VoiceCharacter character, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.VoiceCharacters
            .Include(v => v.References)
            .FirstOrDefaultAsync(v => v.Id == character.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            logger.LogWarning("Update requested for missing voice character {Id}.", character.Id);
            return;
        }

        existing.Name = character.Name;
        existing.Description = character.Description;
        existing.DefaultLanguage = character.DefaultLanguage;
        existing.PreferredEngineId = character.PreferredEngineId;
        existing.ReferenceTranscript = character.ReferenceTranscript;
        existing.AvatarPath = character.AvatarPath;
        existing.Tags = character.Tags;
        existing.Notes = character.Notes;
        existing.IsFavorite = character.IsFavorite;
        existing.EngineMetadata = character.EngineMetadata;
        existing.ModifiedUtc = DateTimeOffset.UtcNow;

        // Reconcile references rather than clear-and-re-add: replacing the collection
        // wholesale would delete and recreate rows, losing identity that engine caches key
        // on and churning the database for no reason.
        var incoming = character.References.ToDictionary(r => r.Id);

        foreach (var stale in existing.References.Where(r => !incoming.ContainsKey(r.Id)).ToList())
            context.VoiceReferences.Remove(stale);

        foreach (var reference in character.References)
        {
            var current = existing.References.FirstOrDefault(r => r.Id == reference.Id);
            if (current is null)
            {
                reference.VoiceCharacterId = existing.Id;
                existing.References.Add(reference);
                continue;
            }

            current.RelativePath = reference.RelativePath;
            current.OriginalFileName = reference.OriginalFileName;
            current.Transcript = reference.Transcript;
            current.Duration = reference.Duration;
            current.SampleRate = reference.SampleRate;
            current.Channels = reference.Channels;
            current.SizeBytes = reference.SizeBytes;
            current.ContentHash = reference.ContentHash;
            current.SortOrder = reference.SortOrder;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.VoiceCharacters
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null) return;

        context.VoiceCharacters.Remove(existing);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // The folder holds the user's recordings. Failing to remove it leaves orphaned
        // files, but failing to remove the database row would leave a broken voice in the
        // library — so the row goes first and a failed directory delete is logged, not
        // thrown.
        var directory = paths.GetVoiceDirectory(id);
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Deleted voice {Id} but could not remove {Directory}.", id, directory);
        }

        logger.LogInformation("Deleted voice character {Id}.", id);
    }

    public async Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.VoiceCharacters
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return;

        existing.IsFavorite = isFavorite;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordUsageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.VoiceCharacters
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return;

        existing.LastUsedUtc = DateTimeOffset.UtcNow;
        existing.GenerationCount++;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
