extern alias KrytenApplication;

using KrytenAssist.Avalonia.DependencyInjection;
using KrytenAssist.Avalonia.Tests.Application.Cruises;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Core.Cruises;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using ListStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseHistoryListStatus;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;
using RecordStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordStatus;

namespace KrytenAssist.Avalonia.Tests.Integration;

public sealed class CruiseHistoryDesktopRestartWorkflowTests
{
    [Fact]
    public async Task DesktopComposition_RecordDisposeRecreateAndLoad_PreservesExactHistoryOffline()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"kryten-037f-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(directory, "desktop.db");
        var observation = CruiseHistoryApplicationTestData.Observation(
            price: 987.46m,
            observedAt: new DateTimeOffset(2026, 7, 17, 14, 25, 0, TimeSpan.FromHours(1)));

        try
        {
            await using (var provider = CreateProvider(databasePath))
            {
                await using var scope = provider.CreateAsyncScope();
                var recorder = scope.ServiceProvider.GetRequiredService<RecordObservation>();

                var result = await recorder.ExecuteAsync(observation);

                Assert.Equal(RecordStatus.FirstObservationRecorded, result.Status);
                Assert.Equal(987.46m, result.Summary!.CurrentPrice!.Amount);
                Assert.Equal("GBP", result.Summary.CurrentPrice.Currency);
                Assert.Equal("per person", result.Summary.CurrentPrice.Basis);
                Assert.True(File.Exists(databasePath));
            }

            await using (var provider = CreateProvider(databasePath))
            {
                await using var scope = provider.CreateAsyncScope();
                var histories = scope.ServiceProvider.GetRequiredService<ListHistories>();

                var result = await histories.ExecuteAsync();

                Assert.Equal(ListStatus.Success, result.Status);
                var details = Assert.Single(result.Histories);
                Assert.Equal(CruiseSailingKey.From(observation), details.History.SailingKey);
                Assert.Equal(observation.Source, details.History.Source);
                var stored = Assert.Single(details.History.Observations);
                Assert.Equal(observation.Snapshot.Offer, stored.Snapshot.Offer);
                Assert.Equal(observation.Snapshot.Prices, stored.Snapshot.Prices);
                Assert.Equal(observation.Snapshot.PromotionSummary, stored.Snapshot.PromotionSummary);
                Assert.Equal(observation.ObservedAt, stored.ObservedAt);
                Assert.Equal(observation.SourceReference, stored.SourceReference);
                Assert.Equal(observation.Source, stored.Source);
                Assert.Equal(observation.ObservedAt, details.Summary.FirstObservedAt);
                Assert.Equal(observation.ObservedAt, details.Summary.LastObservedAt);
                Assert.Equal(987.46m, details.Summary.CurrentPrice!.Amount);
                Assert.Equal(987.46m, details.Summary.LowestPrice!.Amount);
                Assert.Equal(987.46m, details.Summary.HighestPrice!.Amount);
                Assert.Equal(1, details.Summary.ObservationCount);
                Assert.Equal(observation.Snapshot.Offer.ProviderOfferId, details.History.LatestEvidence.ProviderOfferId);
                Assert.Equal(observation.SourceReference, details.History.LatestEvidence.SourceReference);
            }

            Assert.Equal(Path.GetFullPath(databasePath), Path.GetFullPath(Directory.GetFiles(directory).Single(path => path.EndsWith("desktop.db", StringComparison.Ordinal))));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock, FixedClock>();
        services.AddDesktopPersistence(new ConfigurationBuilder().Build(), databasePath);
        return services.BuildServiceProvider();
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 7, 17, 15, 0, 0, TimeSpan.FromHours(1));
    }
}
