using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class SavedCruiseCriteriaAlertDetectorTests
{
    private readonly SavedCruiseCriteriaAlertDetector _detector = new();

    [Fact]
    public void MonthAndBudget_AllMustMatchAndBoundaryPasses()
    {
        var saved = Saved();
        var preferences = new CruisePreferences([12], maximumBudget: new CruiseBudget(900m, "GBP", CruiseBudgetBasis.PerPerson));

        var result = _detector.Detect(saved, preferences, new(), Evidence(new CruisePrice(900m, "GBP", "PER PERSON")));

        result.State.Result.Should().Be(SavedCruiseCriteriaResult.Met);
        result.Candidate.Should().NotBeNull();
        ((CruiseSavedCriteriaAlertDetails)result.Candidate!.Details).MonthConfiguredAndMatched.Should().BeTrue();
    }

    [Theory]
    [InlineData(901, "GBP", "per person")]
    [InlineData(800, "USD", "per person")]
    [InlineData(800, "GBP", "per guest")]
    public void Budget_MismatchDoesNotCreate(decimal amount, string currency, string basis)
    {
        var preferences = new CruisePreferences(maximumBudget: new CruiseBudget(900m, "GBP", CruiseBudgetBasis.PerPerson));
        _detector.Detect(Saved(), preferences, new(), Evidence(new CruisePrice(amount, currency, basis))).Candidate.Should().BeNull();
    }

    [Fact]
    public void Budget_AmbiguousDistinctPricesDoNotCreate()
    {
        var preferences = new CruisePreferences(maximumBudget: new CruiseBudget(900m, "GBP", CruiseBudgetBasis.PerPerson));
        _detector.Detect(Saved(), preferences, new(), Evidence(
            new CruisePrice(800m, "GBP", "per person"), new CruisePrice(850m, "GBP", "per person"))).Candidate.Should().BeNull();
    }

    [Fact]
    public void TotalBudget_RecognizesOnlyExactAliases()
    {
        var preferences = new CruisePreferences(maximumBudget: new CruiseBudget(1800m, "GBP", CruiseBudgetBasis.TotalBooking));
        _detector.Detect(Saved(), preferences, new(), Evidence(new CruisePrice(1800m, "GBP", "total booking"))).Candidate.Should().NotBeNull();
        _detector.Detect(Saved(), preferences, new(), Evidence(new CruisePrice(1700m, "GBP", "total based on two"))).Candidate.Should().BeNull();
    }

    [Fact]
    public void CabinOnly_IsUnknownAndCabinsAlongsideMonthAreExplicitlyUnavailable()
    {
        var cabinOnly = _detector.Detect(Saved(), new CruisePreferences(preferredCabins: [CruiseCabinType.Balcony]), new(), Evidence());
        cabinOnly.State.Result.Should().Be(SavedCruiseCriteriaResult.Unknown);
        cabinOnly.Candidate.Should().BeNull();

        var withMonth = _detector.Detect(Saved(), new CruisePreferences([12], [CruiseCabinType.Balcony]), new(), Evidence());
        ((CruiseSavedCriteriaAlertDetails)withMonth.Candidate!.Details).CabinPreferencesUnavailable.Should().BeTrue();
    }

    [Fact]
    public void DismissedOrDisabledSavedCriteria_AreUnknown()
    {
        var preferences = new CruisePreferences([12]);
        _detector.Detect(Saved(SavedCruiseStatus.Dismissed), preferences, new(), Evidence()).Candidate.Should().BeNull();
        _detector.Detect(Saved(), preferences, new CruiseAlertSettings(savedCriteriaEnabled: false), Evidence()).Candidate.Should().BeNull();
    }

    [Fact]
    public void FirstMetAndNotMetToMetCreateButRepeatedMetDoesNot()
    {
        var saved = Saved();
        var preferences = new CruisePreferences([12]);
        var first = _detector.Detect(saved, preferences, new(), Evidence());
        _detector.Detect(saved, preferences, new(), Evidence(), first.State).Candidate.Should().BeNull();

        var notMet = _detector.Detect(saved, new CruisePreferences([11]), new(), Evidence());
        _detector.Detect(saved, preferences, new(), Evidence(), notMet.State).Candidate.Should().NotBeNull();
    }

    [Fact]
    public void CriteriaFingerprint_IsStableForOrderedEquivalentPreferencesAndChangesWithCriteria()
    {
        var one = SavedCruiseCriteriaAlertDetector.CriteriaFingerprint(new CruisePreferences([12, 1, 12]), new());
        var two = SavedCruiseCriteriaAlertDetector.CriteriaFingerprint(new CruisePreferences([1, 12]), new());
        one.Should().Be(two);
        one.Should().NotBe(SavedCruiseCriteriaAlertDetector.CriteriaFingerprint(new CruisePreferences([1]), new()));
    }

    private static SavedCruise Saved(SavedCruiseStatus status = SavedCruiseStatus.Shortlisted)
    {
        var observation = CruiseHistoryTestData.Observation();
        var snapshot = new SavedCruiseSnapshot(CruiseSailingKey.From(observation), "Cruise", "Operator",
            new CruisePrice(900m, "GBP", "per person"), observation.ObservedAt);
        return new SavedCruise(snapshot, status);
    }

    private static CruiseCriteriaEvidence Evidence(params CruisePrice[] prices) =>
        new(CruiseAlertEvidenceOrigin.RecordedObservation, "evidence", CruiseHistoryTestData.FirstObserved, prices);
}
