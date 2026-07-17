using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseObservationFingerprintOrderingTests
{
    [Fact]
    public void CompareTo_IsDeterministicAndConsistentWithEquality()
    {
        var first = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation());
        var equal = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation(
            observedAt: CruiseHistoryTestData.FirstObserved.AddDays(1),
            providerOfferId: "different"));
        var changed = CruiseObservationFingerprint.From(CruiseHistoryTestData.Observation(
            promotion: "Changed promotion"));

        first.CompareTo(equal).Should().Be(0);
        Math.Sign(first.CompareTo(changed)).Should().Be(-Math.Sign(changed.CompareTo(first)));
        first.CompareTo(null).Should().BePositive();
    }
}
