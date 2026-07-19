extern alias KrytenInfrastructure;
extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using Repository = KrytenInfrastructure::KrytenAssist.Infrastructure.Persistence.SqliteCruiseDiscoveryRepository;
using RecordState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryRecordState;

namespace KrytenAssist.Avalonia.Tests.Infrastructure.Persistence;

public sealed class SqliteCruiseDiscoveryRepositoryTests
{
    private static readonly DateTimeOffset FirstTime = new(2026, 7, 19, 12, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public async Task Baseline_then_later_discovery_round_trips_and_retry_is_idempotent()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var baseline = Check(FirstTime, false, [Occurrence("known", FirstTime)], [new("bad", "missing itinerary id")]);
        var laterTime = FirstTime.AddDays(1);
        var later = Check(laterTime, true, [Occurrence("known", laterTime, "changed-package"), Occurrence("new", laterTime)]);

        await using (var context = database.CreateContext())
        {
            var repository = new Repository(context);
            var first = await repository.RecordAsync(baseline);
            first.State.Should().Be(RecordState.BaselineSeeded);
            first.FirstObservedEvents.Should().BeEmpty();
            (await repository.GetAsync(baseline.Occurrences[0].CatalogueKey.PersistenceKey))!.FirstObservedEventKey.Should().BeNull();

            var discovered = await repository.RecordAsync(later);
            discovered.State.Should().Be(RecordState.RecordedWithFirstObserved);
            discovered.FirstObservedEvents.Should().ContainSingle(x => x.Occurrence.ItineraryKey.ProviderItineraryId == "new");
            var retry = await repository.RecordAsync(later);
            retry.State.Should().Be(RecordState.AlreadyRecorded);
            retry.FirstObservedEvents.Should().ContainSingle().Which.EventKey.Should().Be(discovered.FirstObservedEvents[0].EventKey);
        }

        await using var reopened = database.CreateContext(); var loaded = new Repository(reopened);
        var entries = await loaded.ListFirstObservedAsync();
        entries.Should().ContainSingle(x => x.CatalogueKey.ItineraryKey.ProviderItineraryId == "new");
        var known = await loaded.GetAsync(baseline.Occurrences[0].CatalogueKey.PersistenceKey);
        known!.FirstOccurrence.ProviderOfferId.Should().Be("package");
        known.LatestOccurrence.ProviderOfferId.Should().Be("changed-package");
        known.FirstSeenAt.Should().Be(FirstTime); known.LastSeenAt.Should().Be(laterTime);
        var checks = await loaded.ListChecksAsync();
        checks.Should().HaveCount(2); checks[0].EvidenceKey.Should().Be(later.EvidenceKey);
        checks[1].Rejections.Should().ContainSingle(); checks[1].ObservedAt.Offset.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Known_only_later_check_records_without_first_observed_event()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        await using var context = database.CreateContext(); var repository = new Repository(context);
        await repository.RecordAsync(Check(FirstTime, false, [Occurrence("known", FirstTime)]));

        var result = await repository.RecordAsync(Check(FirstTime.AddHours(1), false, [Occurrence("known", FirstTime.AddHours(1), "new-offer")]));

        result.State.Should().Be(RecordState.RecordedNoNewItineraries);
        result.FirstObservedEvents.Should().BeEmpty();
        (await repository.ListFirstObservedAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Migration_creates_discovery_schema_without_foreign_keys_to_other_features()
    {
        await using var database = new CruisePersistenceTestDatabase(); await database.OpenAndMigrateAsync();
        var tables = new[] { "CruiseDiscoveryScopes", "CruiseDiscoveryScopeCriteria", "CruiseDiscoveryScopeCriterionValues", "CruiseDiscoveryChecks", "CruiseDiscoveryOccurrences", "CruiseDiscoveryRejections", "CruiseItineraryCatalogue" };
        await using var command = database.Connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
        var present = new List<string>(); await using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) present.Add(reader.GetString(0));
        present.Should().Contain(tables);
        foreach (var table in tables)
        {
            await using var fk = database.Connection.CreateCommand(); fk.CommandText = $"PRAGMA foreign_key_list('{table}')";
            await using var reader = await fk.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                reader.GetString(reader.GetOrdinal("table")).Should().Match(name => name.StartsWith("CruiseDiscovery", StringComparison.Ordinal) || name == "CruiseItineraryCatalogue");
        }
    }

    private static CruiseDiscoveryCheck Check(DateTimeOffset time, bool truncated, IEnumerable<CruiseItineraryOccurrence> occurrences, IEnumerable<CruiseDiscoveryRejection>? rejected = null) =>
        new(new CruiseDiscoveryScope(new("tui", "TUI"), "marella", CruiseDiscoverySurface.CruisePackages, 1,
            [new("airport", CruiseDiscoveryCriterionState.Known, ["lgw"]), new("month", CruiseDiscoveryCriterionState.Unknown)]), time, occurrences, rejected, truncated);

    private static CruiseItineraryOccurrence Occurrence(string id, DateTimeOffset time, string offer = "package") =>
        new(new("marella", id), new("tui", "TUI"), time, $"evidence-{id}-{offer}", "Title", "Explorer", new DateOnly(2027, 1, 1), 7, "Palma", "Mediterranean", offer, $"https://www.tui.co.uk/cruise/bookitineraries/example?itineraryCode={id}");
}
