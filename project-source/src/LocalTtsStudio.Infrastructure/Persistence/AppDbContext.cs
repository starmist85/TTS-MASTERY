using System.Text.Json;
using LocalTtsStudio.Core.History;
using LocalTtsStudio.Core.Voices;
using Microsoft.EntityFrameworkCore;

namespace LocalTtsStudio.Infrastructure.Persistence;

/// <summary>
/// SQLite store for voice characters, their references, and generation history.
/// </summary>
/// <remarks>
/// Audio never goes in here. WAV files live on disk and the database holds relative paths
/// to them — a database carrying gigabytes of blobs is slow to open, miserable to back up,
/// and turns a corrupt page into lost recordings rather than a lost index.
/// </remarks>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VoiceCharacter> VoiceCharacters => Set<VoiceCharacter>();
    public DbSet<VoiceReference> VoiceReferences => Set<VoiceReference>();
    public DbSet<GenerationHistoryEntry> GenerationHistory => Set<GenerationHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var jsonStringList = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        var jsonStringDictionary = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        var stringListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (acc, s) => HashCode.Combine(acc, s.GetHashCode())),
            v => v.ToList());

        var stringDictComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<string, string>>(
            (a, b) => a != null && b != null && a.Count == b.Count && !a.Except(b).Any(),
            v => v.Aggregate(0, (acc, kv) => HashCode.Combine(acc, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            v => new Dictionary<string, string>(v));

        modelBuilder.Entity<VoiceCharacter>(entity =>
        {
            entity.ToTable("VoiceCharacters");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.DefaultLanguage).HasMaxLength(16);
            entity.Property(e => e.PreferredEngineId).HasMaxLength(32);
            entity.Property(e => e.Notes).HasMaxLength(8000);
            entity.Property(e => e.AvatarPath).HasMaxLength(400);

            // Tags and engine metadata are small, read with the row, and never queried on
            // their own — a JSON column beats two extra tables and their joins here.
            entity.Property(e => e.Tags)
                  .HasConversion(jsonStringList)
                  .Metadata.SetValueComparer(stringListComparer);

            entity.Property(e => e.EngineMetadata)
                  .HasConversion(jsonStringDictionary)
                  .Metadata.SetValueComparer(stringDictComparer);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsFavorite);
            entity.HasIndex(e => e.ModifiedUtc);

            entity.HasMany(e => e.References)
                  .WithOne()
                  .HasForeignKey(r => r.VoiceCharacterId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoiceReference>(entity =>
        {
            entity.ToTable("VoiceReferences");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RelativePath).IsRequired().HasMaxLength(400);
            entity.Property(e => e.OriginalFileName).HasMaxLength(260);
            entity.Property(e => e.Transcript).HasMaxLength(8000);
            entity.Property(e => e.ContentHash).HasMaxLength(64);

            entity.HasIndex(e => e.VoiceCharacterId);
            entity.HasIndex(e => e.ContentHash);
        });

        modelBuilder.Entity<GenerationHistoryEntry>(entity =>
        {
            entity.ToTable("GenerationHistory");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EngineId).IsRequired().HasMaxLength(32);
            entity.Property(e => e.EngineDisplayName).HasMaxLength(64);
            entity.Property(e => e.VoiceId).HasMaxLength(64);
            entity.Property(e => e.VoiceDisplayName).HasMaxLength(200);
            entity.Property(e => e.CanonicalLanguage).HasMaxLength(16);
            entity.Property(e => e.OutputFilePath).HasMaxLength(400);
            entity.Property(e => e.ErrorCode).HasMaxLength(64);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            // Computed from the file system, so it must not become a column.
            entity.Ignore(e => e.OutputExists);
            entity.Ignore(e => e.TextPreview);

            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => e.EngineId);
        });

        // Same for the display helpers on the voice types.
        modelBuilder.Entity<VoiceCharacter>().Ignore(e => e.HasReferenceAudio).Ignore(e => e.InitialsDisplay);
        modelBuilder.Entity<VoiceReference>().Ignore(e => e.SummaryDisplay);
    }
}
