extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Avalonia.Tests.Application.Cruises;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using GetPreferences = KrytenApplication::KrytenAssist.Application.Cruises.GetCruisePreferences;
using SavePreferences = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferences;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruisePreferencesViewModelTests
{
    [Fact]
    public async Task First_activation_populates_profile_once_and_return_preserves_draft()
    {
        var repository = new FakeCruisePreferencesRepository
        {
            Value = new CruisePreferences(
                [3, 9],
                [CruiseCabinType.Balcony, CruiseCabinType.Suite],
                new CruiseBudget(2500, "gbp", CruiseBudgetBasis.TotalBooking))
        };
        var viewModel = Create(repository);

        await viewModel.ActivateAsync();

        viewModel.MonthOptions.Where(option => option.IsSelected).Select(option => option.Month).Should().Equal(3, 9);
        viewModel.CabinOptions.Where(option => option.IsSelected).Select(option => option.Cabin)
            .Should().Equal(CruiseCabinType.Balcony, CruiseCabinType.Suite);
        viewModel.MaximumBudgetAmount.Should().Be("2500");
        viewModel.MaximumBudgetCurrency.Should().Be("GBP");
        viewModel.BudgetBasis.Should().Be("Total booking");
        viewModel.HasUnsavedChanges.Should().BeFalse();

        viewModel.MonthOptions.Single(option => option.Month == 6).IsSelected = true;
        viewModel.Deactivate();
        await viewModel.ActivateAsync();

        repository.GetCalls.Should().Be(1);
        viewModel.MonthOptions.Single(option => option.Month == 6).IsSelected.Should().BeTrue();
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Explicit_save_normalizes_and_persists_the_complete_draft()
    {
        var repository = new FakeCruisePreferencesRepository();
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();
        viewModel.MonthOptions.Single(option => option.Month == 12).IsSelected = true;
        viewModel.MonthOptions.Single(option => option.Month == 2).IsSelected = true;
        viewModel.CabinOptions.Single(option => option.Cabin == CruiseCabinType.Solo).IsSelected = true;
        viewModel.IsMaximumBudgetEnabled = true;
        viewModel.MaximumBudgetAmount = "3000.50";
        viewModel.MaximumBudgetCurrency = " gbp ";
        viewModel.BudgetBasis = "Per person";

        await viewModel.SaveAsync();

        repository.SaveCalls.Should().Be(1);
        repository.Value.Should().Be(new CruisePreferences(
            [2, 12], [CruiseCabinType.Solo],
            new CruiseBudget(3000.50m, "GBP", CruiseBudgetBasis.PerPerson)));
        viewModel.HasUnsavedChanges.Should().BeFalse();
        viewModel.Message.Should().Be(
            "Cruise preferences saved. 0 shortlisted sailings were evaluated; 0 alerts were created, 0 evaluations failed.");
    }

    [Theory]
    [InlineData(null, "GBP", "Per person", "Enter a maximum budget amount.")]
    [InlineData("-1", "GBP", "Per person", "Maximum budget must be a non-negative number.")]
    [InlineData("not money", "GBP", "Per person", "Maximum budget must be a non-negative number.")]
    [InlineData("100", "GB", "Per person", "Currency must contain exactly three letters.")]
    [InlineData("100", "GBP", "Unknown", "Choose whether the budget is per person or total booking.")]
    public async Task Invalid_budget_retains_draft_without_calling_save(
        string? amount,
        string currency,
        string basis,
        string expectedError)
    {
        var repository = new FakeCruisePreferencesRepository();
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();
        viewModel.IsMaximumBudgetEnabled = true;
        viewModel.MaximumBudgetAmount = amount;
        viewModel.MaximumBudgetCurrency = currency;
        viewModel.BudgetBasis = basis;

        await viewModel.SaveAsync();

        repository.SaveCalls.Should().Be(0);
        new[] { viewModel.BudgetAmountError, viewModel.CurrencyError, viewModel.BasisError }
            .Should().Contain(expectedError);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Clear_draft_requires_save_and_cancel_restores_confirmed_values()
    {
        var confirmed = new CruisePreferences([8], [CruiseCabinType.Outside], new CruiseBudget(0, "GBP", CruiseBudgetBasis.PerPerson));
        var repository = new FakeCruisePreferencesRepository { Value = confirmed };
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();

        viewModel.ClearDraft();

        repository.SaveCalls.Should().Be(0);
        viewModel.SelectedMonthCount.Should().Be(0);
        viewModel.SelectedCabinCount.Should().Be(0);
        viewModel.IsMaximumBudgetEnabled.Should().BeFalse();
        viewModel.HasUnsavedChanges.Should().BeTrue();

        viewModel.CancelChanges();
        viewModel.MonthOptions.Single(option => option.Month == 8).IsSelected.Should().BeTrue();
        viewModel.CabinOptions.Single(option => option.Cabin == CruiseCabinType.Outside).IsSelected.Should().BeTrue();
        viewModel.IsMaximumBudgetEnabled.Should().BeTrue();
        viewModel.MaximumBudgetAmount.Should().Be("0");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Failed_save_retains_dirty_draft_for_retry()
    {
        var repository = new FakeCruisePreferencesRepository();
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();
        viewModel.MonthOptions[0].IsSelected = true;
        repository.Exception = new InvalidOperationException();

        await viewModel.SaveAsync();

        viewModel.HasUnsavedChanges.Should().BeTrue();
        viewModel.ErrorMessage.Should().Contain("could not be saved locally");
    }

    private static CruisePreferencesViewModel Create(FakeCruisePreferencesRepository repository) =>
        new(
            new GetPreferences(repository),
            CruiseCriteriaTestFactory.CreateSavePreferences(
                new FakeSavedCruiseRepository(),
                repository,
                new FakeCruiseObservationRepository()),
            new FixedClock());

    private sealed class FixedClock : KrytenAssist.Avalonia.Tools.IClock
    {
        public DateTimeOffset Now => new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);
    }
}
