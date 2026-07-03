using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class KrytenAssistDbContextFactory : IDesignTimeDbContextFactory<KrytenAssistDbContext>
{
    public KrytenAssistDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KrytenAssistDbContext>();

        optionsBuilder.UseSqlite("Data Source=krytenassist.db");

        return new KrytenAssistDbContext(optionsBuilder.Options);
    }
}