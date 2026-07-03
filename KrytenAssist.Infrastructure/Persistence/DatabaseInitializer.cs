using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Initialise()
    {
        var options = new DbContextOptionsBuilder<KrytenAssistDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        using var dbContext = new KrytenAssistDbContext(options);
        dbContext.Database.Migrate();
    }
}