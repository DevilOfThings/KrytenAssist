using KrytenAssist.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class KrytenAssistDbContext : DbContext
{
    public KrytenAssistDbContext(DbContextOptions<KrytenAssistDbContext> options)
        : base(options)
    {
    }

    public DbSet<PromptCard> PromptCards => Set<PromptCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KrytenAssistDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}