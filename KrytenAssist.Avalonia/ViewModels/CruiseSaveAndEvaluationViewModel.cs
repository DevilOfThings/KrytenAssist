extern alias KrytenApplication;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Core.Cruises;
using GetSaved = KrytenApplication::KrytenAssist.Application.Cruises.GetSavedCruise;
using ListShips = KrytenApplication::KrytenAssist.Application.Cruises.ListFavouriteCruiseShips;
using MutationStatus = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseMutationStatus;
using PreferenceStatus = KrytenApplication::KrytenAssist.Application.Cruises.PersonalCruisePreferenceMutationStatus;
using QueryStatus = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseQueryStatus;
using SaveCruiseUseCase = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruise;
using SetFavourite = KrytenApplication::KrytenAssist.Application.Cruises.SetSavedCruiseFavourite;
using SetShip = KrytenApplication::KrytenAssist.Application.Cruises.SetFavouriteCruiseShip;
using SnapshotFactory = KrytenApplication::KrytenAssist.Application.Cruises.SavedCruiseSnapshotFactory;
using UpdateEvaluation = KrytenApplication::KrytenAssist.Application.Cruises.UpdateCruiseEvaluation;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class CruiseSaveAndEvaluationViewModel : INotifyPropertyChanged
{
    private readonly SaveCruiseUseCase _saveCruise; private readonly GetSaved _getSaved; private readonly UpdateEvaluation _updateEvaluation;
    private readonly SetFavourite _setFavourite; private readonly SetShip _setShip; private readonly ListShips _listShips; private readonly SnapshotFactory _factory; private readonly IClock _clock;
    private readonly AsyncCommand _saveEvaluationCommand; private readonly DelegateCommand _cancelChangesCommand; private readonly AsyncCommand _toggleFavouriteCommand; private readonly AsyncCommand _toggleShipCommand; private CancellationTokenSource? _cancellation; private int _generation;
    private CruiseObservation? _target; private SavedCruise? _saved; private bool _isBusy; private bool _isEditorOpen; private string? _message; private string _interest = "Unrated"; private string _overall = "Unrated"; private string _itinerary = "Unrated"; private string _ship = "Unrated"; private string _value = "Unrated"; private string? _notes; private bool _isShipFavourite;

    public CruiseSaveAndEvaluationViewModel(SaveCruiseUseCase saveCruise, GetSaved getSaved, UpdateEvaluation updateEvaluation, SetFavourite setFavourite, SetShip setShip, ListShips listShips, SnapshotFactory factory, IClock clock)
    { _saveCruise=saveCruise; _getSaved=getSaved; _updateEvaluation=updateEvaluation; _setFavourite=setFavourite; _setShip=setShip; _listShips=listShips; _factory=factory; _clock=clock;
      _saveEvaluationCommand=new AsyncCommand(SaveEvaluationAsync,()=>CanEdit); _cancelChangesCommand=new DelegateCommand(CancelChanges,()=>IsEditorOpen&&!IsBusy); _toggleFavouriteCommand=new AsyncCommand(ToggleFavouriteAsync,()=>CanEdit); _toggleShipCommand=new AsyncCommand(ToggleShipAsync,()=>CanEdit); }

    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<string> InterestOptions { get; } = ["Unrated", "Maybe", "Strong candidate"];
    public IReadOnlyList<string> RatingOptions { get; } = ["Unrated", "1", "2", "3", "4", "5"];
    public ICommand SaveEvaluationCommand=>_saveEvaluationCommand; public ICommand CancelChangesCommand=>_cancelChangesCommand; public ICommand ToggleFavouriteCommand=>_toggleFavouriteCommand; public ICommand ToggleShipFavouriteCommand=>_toggleShipCommand;
    public bool HasTarget=>_target is not null; public bool IsSaved=>_saved is not null; public bool IsEditorOpen { get=>_isEditorOpen; private set=>Set(ref _isEditorOpen,value); }
    public bool IsBusy { get=>_isBusy; private set { if(Set(ref _isBusy,value)) RaiseCommands(); } } public bool CanEdit=>IsSaved&&IsEditorOpen&&!IsBusy;
    public string? TargetTitle=>_target?.Snapshot.Offer.Title; public string? TargetIdentity=>_target is null?null:$"{_target.Snapshot.Offer.ShipName} · {_target.Snapshot.Offer.DepartureDate:d MMM yyyy}";
    public string? Message { get=>_message; private set=>Set(ref _message,value); }
    public string Interest { get=>_interest; set=>Set(ref _interest,value); } public string OverallRating { get=>_overall; set=>Set(ref _overall,value); } public string ItineraryRating { get=>_itinerary; set=>Set(ref _itinerary,value); } public string ShipRating { get=>_ship; set=>Set(ref _ship,value); } public string ValueRating { get=>_value; set=>Set(ref _value,value); }
    public string? Notes { get=>_notes; set { if(Set(ref _notes,value)) OnPropertyChanged(nameof(NotesCountText)); } } public string NotesCountText=>$"{Notes?.Length??0} / {CruiseEvaluation.MaximumNotesLength}";
    public bool IsFavourite=>_saved?.IsFavourite==true; public bool IsShipFavourite { get=>_isShipFavourite; private set=>Set(ref _isShipFavourite,value); }
    public string FavouriteButtonText=>IsFavourite?"Remove Cruise Favourite":"Favourite Cruise"; public string ShipFavouriteButtonText=>IsShipFavourite?"Remove Ship Favourite":"Favourite Ship";

    public async Task<string> SaveAndEditAsync(CruiseObservation observation)
    { ArgumentNullException.ThrowIfNull(observation); var (generation, token)=BeginTarget(observation); IsBusy=true; Message="Saving cruise…";
      try { var result=await _saveCruise.ExecuteAsync(_factory.Create(observation,_clock.Now),token); if(generation!=_generation)return "Save result ignored because the capture changed.";
        if(result.Status is MutationStatus.Created or MutationStatus.Updated or MutationStatus.Unchanged) { _saved=result.SavedCruise; await LoadShipFavouriteAsync(generation,token); ApplySaved(); IsEditorOpen=true; Message=result.Status switch { MutationStatus.Created=>"Cruise saved to your shortlist.", MutationStatus.Updated=>"Saved cruise details refreshed; your evaluation was preserved.", _=>"This cruise is already saved."}; NotifySaved(); return Message; }
        Message=result.Status==MutationStatus.Cancelled?"Saving this cruise was cancelled. You can try again.":"This cruise could not be saved locally. Please try again."; return Message;
      } finally { if(generation==_generation)IsBusy=false; } }

    public async Task InspectAsync(CruiseObservation observation)
    { var (generation,token)=BeginTarget(observation); try { var result=await _getSaved.ExecuteAsync(CruiseSailingKey.From(observation),token); if(generation!=_generation)return; _saved=result.Status==QueryStatus.Found?result.SavedCruise:null; IsEditorOpen=false; Message=null; NotifySaved(); } catch { if(generation==_generation){_saved=null;NotifySaved();} } }
    public void OpenEditor() { if(_saved is null)return; ApplySaved(); IsEditorOpen=true; Message=null; RaiseCommands(); }
    public void ClearTarget() { _generation++; _cancellation?.Cancel(); _cancellation?.Dispose(); _cancellation=null; _target=null; _saved=null; IsEditorOpen=false; Message=null; NotifyTarget(); }
    public void Deactivate()=>ClearTarget();

    private async Task SaveEvaluationAsync()
    { if(_saved is null||_target is null)return; if(Notes?.Length>CruiseEvaluation.MaximumNotesLength){Message="Notes cannot exceed 4,000 characters.";return;} var generation=_generation; IsBusy=true;
      var evaluation=new CruiseEvaluation(ParseInterest(Interest),ParseRating(OverallRating),ParseRating(ItineraryRating),ParseRating(ShipRating),ParseRating(ValueRating),Notes);
      try { var result=await _updateEvaluation.ExecuteAsync(_saved.SailingKey,evaluation,_cancellation?.Token??default); if(generation!=_generation)return; if(result.Status is MutationStatus.Updated or MutationStatus.Unchanged){_saved=result.SavedCruise;Message=result.Status==MutationStatus.Updated?"Evaluation saved.":"No evaluation changes were needed.";} else Message=result.Status==MutationStatus.NotFound?"This saved cruise is no longer available.":result.Status==MutationStatus.Cancelled?"Saving the evaluation was cancelled.":"The evaluation could not be saved locally."; }
      finally { if(generation==_generation)IsBusy=false; } }
    private async Task ToggleFavouriteAsync()=>await ToggleAsync(false);
    private async Task ToggleShipAsync()=>await ToggleAsync(true);
    private async Task ToggleAsync(bool shipFavourite)
    { if(_saved is null)return; var generation=_generation; IsBusy=true; try { if(shipFavourite){var desired=!IsShipFavourite;var r=await _setShip.ExecuteAsync(CruiseShipKey.From(_saved.SailingKey),desired,_cancellation?.Token??default);if(generation!=_generation)return;if(r.Status is PreferenceStatus.Updated or PreferenceStatus.Unchanged){IsShipFavourite=desired;Message=desired?"Ship marked as favourite.":"Ship removed from favourites.";}else Message="The ship favourite could not be changed.";}else{var desired=!IsFavourite;var r=await _setFavourite.ExecuteAsync(_saved.SailingKey,desired,_cancellation?.Token??default);if(generation!=_generation)return;if(r.Status is MutationStatus.Updated or MutationStatus.Unchanged){_saved=r.SavedCruise;Message=desired?"Cruise marked as favourite.":"Cruise removed from favourites.";NotifySaved();}else Message="The cruise favourite could not be changed.";} } finally {if(generation==_generation)IsBusy=false;} }
    private async Task LoadShipFavouriteAsync(int generation,CancellationToken token){var r=await _listShips.ExecuteAsync(token);if(generation==_generation)IsShipFavourite=r.Ships.Contains(CruiseShipKey.From(_saved!.SailingKey));}
    private (int,CancellationToken) BeginTarget(CruiseObservation observation){_generation++;_cancellation?.Cancel();_cancellation?.Dispose();_cancellation=new CancellationTokenSource();_target=observation;_saved=null;IsEditorOpen=false;NotifyTarget();return(_generation,_cancellation.Token);}
    private void ApplySaved(){var e=_saved!.Evaluation;Interest=e.InterestLevel switch{CruiseInterestLevel.Maybe=>"Maybe",CruiseInterestLevel.StrongCandidate=>"Strong candidate",_=>"Unrated"};OverallRating=Format(e.OverallRating);ItineraryRating=Format(e.ItineraryRating);ShipRating=Format(e.ShipRating);ValueRating=Format(e.ValueRating);Notes=e.Notes;}
    private void CancelChanges(){if(_saved is not null)ApplySaved();Message="Changes cancelled.";}
    private void NotifyTarget(){OnPropertyChanged(nameof(HasTarget));OnPropertyChanged(nameof(TargetTitle));OnPropertyChanged(nameof(TargetIdentity));NotifySaved();}
    private void NotifySaved(){OnPropertyChanged(nameof(IsSaved));OnPropertyChanged(nameof(IsFavourite));OnPropertyChanged(nameof(FavouriteButtonText));OnPropertyChanged(nameof(ShipFavouriteButtonText));RaiseCommands();}
    private void RaiseCommands(){_saveEvaluationCommand.RaiseCanExecuteChanged();_cancelChangesCommand.RaiseCanExecuteChanged();_toggleFavouriteCommand.RaiseCanExecuteChanged();_toggleShipCommand.RaiseCanExecuteChanged();}
    private static string Format(int? value)=>value?.ToString()??"Unrated"; private static int? ParseRating(string value)=>int.TryParse(value,out var result)?result:null; private static CruiseInterestLevel? ParseInterest(string value)=>value=="Maybe"?CruiseInterestLevel.Maybe:value=="Strong candidate"?CruiseInterestLevel.StrongCandidate:null;
    private bool Set<T>(ref T field,T value,[CallerMemberName]string? name=null){if(EqualityComparer<T>.Default.Equals(field,value))return false;field=value;OnPropertyChanged(name);return true;} private void OnPropertyChanged([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new(name));
    private sealed class DelegateCommand(Action action,Func<bool> can):ICommand{public event EventHandler? CanExecuteChanged;public bool CanExecute(object? p)=>can();public void Execute(object? p){if(CanExecute(p))action();}public void RaiseCanExecuteChanged()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);}
    private sealed class AsyncCommand(Func<Task> action,Func<bool> can):ICommand{private bool running;public event EventHandler? CanExecuteChanged;public bool CanExecute(object? p)=>!running&&can();public async void Execute(object? p){if(!CanExecute(p))return;running=true;RaiseCanExecuteChanged();try{await action();}catch{}finally{running=false;RaiseCanExecuteChanged();}}public void RaiseCanExecuteChanged()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);}
}
