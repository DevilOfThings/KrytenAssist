using FluentAssertions;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Core.Tests.Cruises;

public sealed class CruiseEvaluationTests
{
    [Fact]
    public void Empty_evaluation_does_not_invent_personal_answers() =>
        CruiseEvaluation.Empty.Should().Be(new CruiseEvaluation());

    [Fact]
    public void Constructor_preserves_optional_ratings_and_normalizes_notes()
    {
        var value = new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, 5, 4, 3, 2, " Great option ");
        value.Should().Be(new CruiseEvaluation(CruiseInterestLevel.StrongCandidate, 5, 4, 3, 2, "Great option"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Ratings_outside_one_to_five_are_rejected(int rating) =>
        FluentActions.Invoking(() => new CruiseEvaluation(overallRating: rating)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Invalid_interest_and_long_notes_are_rejected()
    {
        FluentActions.Invoking(() => new CruiseEvaluation((CruiseInterestLevel)99)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new CruiseEvaluation(notes: new string('x', 4001))).Should().Throw<ArgumentException>();
    }
}
