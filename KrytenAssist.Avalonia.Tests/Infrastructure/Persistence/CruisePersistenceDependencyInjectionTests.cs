extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContract = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseObservationRepository;
using InfrastructureRegistration = KrytenInfrastructure::KrytenAssist.Infrastructure.DependencyInjection;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseObservationRepository;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class CruisePersistenceDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ResolvesCruiseRepositoryFromIsolatedDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"kryten-037c-{Guid.NewGuid():N}.db");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PromptCards"] = $"Data Source={databasePath}"
                })
                .Build();
            var services = new ServiceCollection();

            InfrastructureRegistration.AddInfrastructure(services, configuration);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<RepositoryContract>();

            Assert.IsType<Repository>(repository);
            Assert.True(File.Exists(databasePath));
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
