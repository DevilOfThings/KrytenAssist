extern alias KrytenInfrastructure;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DatabaseContext = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.KrytenAssistDbContext;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

internal sealed class CruisePersistenceTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public async Task OpenAndMigrateAsync()
    {
        await _connection.OpenAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task OpenAsync() => await _connection.OpenAsync();

    public SqliteConnection Connection => _connection;

    public DatabaseContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .EnableDetailedErrors()
            .Options;
        return new DatabaseContext(options);
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
