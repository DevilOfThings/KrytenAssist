using FluentValidation;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PromptCards.CreatePromptCard>();
        services.AddScoped<PromptCards.UpdatePromptCard>();
        services.AddScoped<PromptCards.DeletePromptCard>();
        services.AddSingleton<CruisePriceHistoryAnalyzer>();
        services.AddScoped<RecordCruiseObservation>();
        services.AddScoped<GetCruiseHistory>();
        services.AddScoped<ListCruiseHistories>();
        services.AddScoped<SaveCruise>();
        services.AddScoped<UpdateCruiseEvaluation>();
        services.AddScoped<DismissCruise>();
        services.AddScoped<RestoreCruise>();
        services.AddScoped<RemoveSavedCruise>();
        services.AddScoped<SetSavedCruiseFavourite>();
        services.AddScoped<GetSavedCruise>();
        services.AddScoped<ListSavedCruises>();
        services.AddScoped<ListSavedCruiseDetails>();
        services.AddScoped<SetFavouriteCruiseShip>();
        services.AddScoped<ListFavouriteCruiseShips>();
        services.AddScoped<GetCruisePreferences>();
        services.AddScoped<SaveCruisePreferences>();
        services.AddSingleton<SavedCruiseSnapshotFactory>();
        services.AddSingleton<CruiseObservationAlertDetector>();
        services.AddSingleton<SavedCruiseCriteriaAlertDetector>();
        services.AddSingleton<CruiseCriteriaEvidenceSelector>();
        services.AddScoped<ListCruiseAlerts>();
        services.AddScoped<GetCruiseAlert>();
        services.AddScoped<CountUnreadCruiseAlerts>();
        services.AddScoped<ChangeCruiseAlertStatus>();
        services.AddScoped<GetCruiseAlertSettings>();
        services.AddScoped<SaveCruiseAlertSettings>();
        services.AddScoped<MaterializeCruiseAlertCandidates>();
        services.AddScoped<EvaluateRecordedCruiseAlerts>();
        services.AddScoped<EvaluateSavedCruiseCriteriaAlerts>();
        services.AddScoped<RecordCruiseObservationAndEvaluateAlerts>();
        services.AddScoped<EvaluateSavedCruiseCriteriaForSavedCruise>();
        services.AddScoped<EvaluateSavedCruiseCriteriaForSailing>();
        services.AddScoped<SaveCruiseAndEvaluateCriteria>();
        services.AddScoped<RestoreCruiseAndEvaluateCriteria>();
        services.AddScoped<SaveCruisePreferencesAndEvaluateCriteria>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
