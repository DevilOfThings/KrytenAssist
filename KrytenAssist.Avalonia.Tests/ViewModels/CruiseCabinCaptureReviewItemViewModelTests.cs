extern alias KrytenApplication;

using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using Candidate = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinCaptureCandidateResult;
using RecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordAndAlertResult;
using CabinRecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordResult;
using CabinStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinOperationStatus;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseCabinCaptureReviewItemViewModelTests
{
    [Fact]
    public void ReadyEvidence_PresentsHonestPartialContextWithoutRecording()
    {
        var observation = Observation();
        var called = false;
        var item = new CruiseCabinCaptureReviewItemViewModel(
            Candidate.Ready("Iconic Islands", observation.SourceReference!, observation),
            _ => { called = true; return Task.FromResult(Result(observation)); });

        Assert.True(item.CanRecord);
        Assert.Contains("Inside — Available when captured for this search", item.StatesText);
        Assert.Contains("Outside — Unknown", item.StatesText);
        Assert.Equal("Partial evidence", item.CoverageText);
        Assert.Contains("2 adults", item.ContextText);
        Assert.False(called);
    }

    [Fact]
    public async Task RecordCommand_RecordsOnlyThisCabinObservationAndReportsSuccess()
    {
        var observation = Observation();
        CruiseCabinObservation? recorded = null;
        var item = new CruiseCabinCaptureReviewItemViewModel(
            Candidate.Ready("Iconic Islands", observation.SourceReference!, observation),
            value => { recorded = value; return Task.FromResult(Result(value)); });

        item.RecordCommand.Execute(null);
        for (var index = 0; index < 20 && item.IsRecording; index++) await Task.Delay(10);

        Assert.Same(observation, recorded);
        Assert.Contains("First cabin observation recorded", item.RecordingMessage);
    }

    private static RecordResult Result(CruiseCabinObservation observation) => new(
        CabinRecordResult.Recorded(CabinStatus.FirstObservationRecorded,
            new CruiseCabinHistoryAnalyzer().Analyze([observation])), null, null);

    private static CruiseCabinObservation Observation()
    {
        var states = Enum.GetValues<CruiseCabinType>().Select(type => new CruiseCabinState(type,
            type == CruiseCabinType.Inside ? CruiseCabinAvailabilityState.Available : CruiseCabinAvailabilityState.Unknown));
        return new(new CruiseSailingKey("marella", "Marella Voyager", new DateOnly(2026, 10, 2), 7),
            new CruiseSource("tui", "TUI"), new CruiseCabinSearchContext(2, 0, [], true,
                CruiseCabinPackageMode.FlyCruise, "STN", 1), CruiseCabinEvidenceCoverage.Partial,
            states, new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero), "evidence",
            "https://www.tui.co.uk/cruise/bookitineraries/test?itineraryCodeOne=1");
    }
}
