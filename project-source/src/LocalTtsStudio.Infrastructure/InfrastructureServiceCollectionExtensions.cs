using LocalTtsStudio.Core.Abstractions;
using LocalTtsStudio.Core.Settings;
using LocalTtsStudio.Infrastructure.Configuration;
using LocalTtsStudio.Infrastructure.Persistence;
using LocalTtsStudio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalTtsStudio.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Register persistence, configuration and file services.
    /// </summary>
    /// <remarks>
    /// <paramref name="paths"/> is passed in already constructed rather than resolved from
    /// the container, because the database connection string and the settings file
    /// location both depend on it — it has to exist before anything else can be
    /// registered.
    /// </remarks>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IAppPaths paths)
    {
        services.AddSingleton(paths);

        // A context factory rather than a scoped DbContext: WPF has no request scope, and
        // a single long-lived context shared across view-models accumulates tracked
        // entities and cross-thread access bugs. Each operation takes its own short-lived
        // context.
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={paths.DatabasePath}");
            options.EnableSensitiveDataLogging(false);
        });

        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IVoiceCharacterRepository, VoiceCharacterRepository>();
        services.AddSingleton<IGenerationHistoryRepository, GenerationHistoryRepository>();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IOutputFileNamingService, OutputFileNamingService>();
        services.AddSingleton<EngineConfigurationLoader>();

        return services;
    }
}
