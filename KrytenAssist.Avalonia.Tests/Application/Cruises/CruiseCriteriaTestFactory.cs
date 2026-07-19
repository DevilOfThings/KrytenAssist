extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CriteriaStateRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ISavedCruiseCriteriaStateRepository;
using EvaluateCriteria = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateSavedCruiseCriteriaAlerts;
using EvaluateForSaved = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateSavedCruiseCriteriaForSavedCruise;
using EvaluateForSailing = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateSavedCruiseCriteriaForSailing;
using GetPreferences = KrytenApplication::KrytenAssist.Application.Cruises.GetCruisePreferences;
using GetSaved = KrytenApplication::KrytenAssist.Application.Cruises.GetSavedCruise;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using ListCabinHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseCabinHistories;
using ListSaved = KrytenApplication::KrytenAssist.Application.Cruises.ListSavedCruises;
using Materialize = KrytenApplication::KrytenAssist.Application.Cruises.MaterializeCruiseAlertCandidates;
using Restore = KrytenApplication::KrytenAssist.Application.Cruises.RestoreCruise;
using RestoreAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.RestoreCruiseAndEvaluateCriteria;
using Save = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruise;
using SaveAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruiseAndEvaluateCriteria;
using SavePreferences = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferences;
using SavePreferencesAndEvaluate = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruisePreferencesAndEvaluateCriteria;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

internal static class CruiseCriteriaTestFactory
{
    internal static EvaluateForSaved CreateEvaluator(
        FakeCruisePreferencesRepository preferences,
        FakeCruiseObservationRepository observations,
        FakeCruiseCabinObservationRepository? cabins = null,
        TestAlertRepository? alerts = null) =>
        new(
            new KrytenApplication::KrytenAssist.Application.Cruises.CruiseCriteriaEvidenceSelector(),
            new EvaluateCriteria(
                new SavedCruiseCriteriaAlertDetector(),
                new TestAlertSettingsRepository(),
                new TestCriteriaStateRepository(),
                new Materialize(alerts ?? new TestAlertRepository())),
            new GetPreferences(preferences),
            new ListHistories(observations, new CruisePriceHistoryAnalyzer()),
            new ListCabinHistories(cabins ?? new FakeCruiseCabinObservationRepository(), new CruiseCabinHistoryAnalyzer()));

    internal static EvaluateForSailing CreateForSailing(
        FakeSavedCruiseRepository saved,
        FakeCruisePreferencesRepository preferences,
        FakeCruiseObservationRepository observations,
        FakeCruiseCabinObservationRepository? cabins = null,
        TestAlertRepository? alerts = null) =>
        new(new GetSaved(saved), CreateEvaluator(preferences, observations, cabins, alerts));

    internal static SaveAndEvaluate CreateSave(
        FakeSavedCruiseRepository saved,
        FakeCruisePreferencesRepository preferences,
        FakeCruiseObservationRepository observations,
        TestAlertRepository? alerts = null) =>
        new(new Save(saved), CreateEvaluator(preferences, observations, alerts: alerts));

    internal static RestoreAndEvaluate CreateRestore(
        FakeSavedCruiseRepository saved,
        FakeCruisePreferencesRepository preferences,
        FakeCruiseObservationRepository observations,
        TestAlertRepository? alerts = null) =>
        new(new Restore(saved), CreateEvaluator(preferences, observations, alerts: alerts));

    internal static SavePreferencesAndEvaluate CreateSavePreferences(
        FakeSavedCruiseRepository saved,
        FakeCruisePreferencesRepository preferences,
        FakeCruiseObservationRepository observations,
        TestAlertRepository? alerts = null) =>
        new(
            new SavePreferences(preferences),
            new ListSaved(saved),
            new ListHistories(observations, new CruisePriceHistoryAnalyzer()),
            new ListCabinHistories(new FakeCruiseCabinObservationRepository(), new CruiseCabinHistoryAnalyzer()),
            CreateEvaluator(preferences, observations, alerts: alerts));
}

internal sealed class TestCriteriaStateRepository : CriteriaStateRepository
{
    private readonly Dictionary<(CruiseSailingKey Key, string Fingerprint), SavedCruiseCriteriaEvaluationState> _states = [];
    internal int UpsertCalls { get; private set; }

    public Task<SavedCruiseCriteriaEvaluationState?> GetAsync(
        CruiseSailingKey sailingKey,
        string criteriaFingerprint,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_states.GetValueOrDefault((sailingKey, criteriaFingerprint)));

    public Task UpsertAsync(
        SavedCruiseCriteriaEvaluationState state,
        CancellationToken cancellationToken = default)
    {
        UpsertCalls++;
        _states[(state.SailingKey, state.CriteriaFingerprint)] = state;
        return Task.CompletedTask;
    }
}
