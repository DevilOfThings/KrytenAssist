extern alias KrytenInfrastructure;

using Microsoft.EntityFrameworkCore;
using DatabaseContext = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.KrytenAssistDbContext;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

internal sealed class CruisePersistenceFileDatabase : IAsyncDisposable
{
    public CruisePersistenceFileDatabase()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"kryten-cruise-{Guid.NewGuid():N}.db");
        ConnectionString = $"Data Source={Path};Default Timeout=1;Pooling=False";
    }

    public string Path { get; }
    public string ConnectionString { get; }

    public async Task MigrateAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public DatabaseContext CreateContext() =>
        new(new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(ConnectionString)
            .EnableDetailedErrors()
            .Options);

    public Repository CreateRepository(DatabaseContext context) => new(context);

    public ValueTask DisposeAsync()
    {
        DeleteIfPresent(Path);
        DeleteIfPresent($"{Path}-wal");
        DeleteIfPresent($"{Path}-shm");
        return ValueTask.CompletedTask;
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
