using Microsoft.Maui.Controls;
using Org.BouncyCastle.Math.Field;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.Db;
using Zaczy.SongBook.MAUI.Services;
using Zaczy.SongBook.MAUI.ViewModels;
using Zaczy.SongBook.Models;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class SingingGroupsPage : ContentPage, INotifyPropertyChanged
{
    public ObservableCollection<SingingGroupEntity> Groups { get; } = new();

    private readonly SingingGroupRepositoryLite? _repo;
    private UserViewModel? _userViewModel;
    private readonly Settings? _settings;
    private readonly ListenersGroupBroadcastService _listenersGroupBroadcastService;
    private bool _duringGroupCreation;
    public UserViewModel? UserViewModel
    {
        get => _userViewModel;
        set 
        { 
            if (_userViewModel != value)
            {
                _userViewModel = value;
                OnPropertyChanged(nameof(UserViewModel));
            }
        }

    }

    public System.Windows.Input.ICommand FetchGroupsCommand { get; }
    public System.Windows.Input.ICommand CreateGroupCommand { get; }

    public SingingGroupsPage(ListenersGroupBroadcastService listenersGroupBroadcastService)
    {
        FetchGroupsCommand = new Command(async () => await RefreshFromApiAsync());
        CreateGroupCommand = new Command(async () => await CreateGroupAsync());
        _listenersGroupBroadcastService = listenersGroupBroadcastService;

        InitializeComponent();
        BindingContext = this;

        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            _repo = services?.GetService(typeof(SingingGroupRepositoryLite)) as SingingGroupRepositoryLite;
            UserViewModel = services?.GetService(typeof(UserViewModel)) as UserViewModel;
            _settings = services?.GetService(typeof(Microsoft.Extensions.Options.IOptions<Settings>)) is
                Microsoft.Extensions.Options.IOptions<Settings> opt ? opt.Value : null;
        }
        catch
        {
            _repo = null;
        }

        _ = LoadAsync();
    }

    public bool AmIDirector
    {
        get => _listenersGroupBroadcastService.AmILeader;
        set
        {
            if (_listenersGroupBroadcastService.AmILeader != value)
            {
                _listenersGroupBroadcastService.AmILeader = value;
                OnPropertyChanged(nameof(AmIDirector));
            }
        }

    }

    /// <summary>
    /// Załaduj listę grup z lokalnej bazy danych. Jeśli baza jest pusta, spróbuj załadować z API. 
    /// Obsłuż błędy i pokaż komunikaty użytkownikowi.
    /// </summary>
    private async Task LoadAsync()
    {
        try
        {
            if (_repo == null)
            {
                await DisplayAlert("Błąd", "Repozytorium grup nie jest dostępne.", "OK");
                return;
            }

            if (!_repo.HasGroups)
                await _repo.LoadGroupsFromApiAsync();

            var list = await _repo.GetAllAsync();
            Groups.Clear();
            foreach (var g in list.OrderBy(gr => !gr.IsLocalOnly))
                Groups.Add(g);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Nie udało się załadować grup: {ex.Message}", "OK");
        }

        OnPropertyChanged(nameof(IsEmptyGroups));
    }

    /// <summary>
    /// Odśwież z API
    /// </summary>
    private async Task RefreshFromApiAsync()
    {
        try
        {
            IsBusy = true;

            if (_repo == null)
            {
                await DisplayAlert("Błąd", "Repozytorium grup nie jest dostępne.", "OK");
                return;
            }

            await _repo.DeleteAllAsync();
            await _repo.LoadGroupsFromApiAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Houston, mamy problem!", $"Błąd odświeżania grup: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Tworzy nową grupę — pyta użytkownika o nazwę, opis i datę wygaśnięcia, wysyła na serwer, a następnie odświeża listę.
    /// </summary>
    private async Task CreateGroupAsync()
    {
        try
        {
            if (_duringGroupCreation == true)
                return;

            if (_settings == null)
            {
                await DisplayAlert("Błąd", "Ustawienia aplikacji nie są dostępne.", "OK");
                return;
            }

            _duringGroupCreation = true;

            var name = await DisplayPromptAsync(
                "Nowa grupa",
                "Podaj nazwę grupy:",
                accept: "Dalej",
                cancel: "Anuluj",
                maxLength: 255,
                keyboard: Keyboard.Text);

            if (string.IsNullOrWhiteSpace(name))
                return;

            var description = await DisplayPromptAsync(
                "Nowa grupa",
                "Podaj opis grupy (opcjonalnie):",
                accept: "Dalej",
                cancel: "Anuluj",
                keyboard: Keyboard.Text);

            if (description == null) // użytkownik nacisnął Anuluj
                return;

            var validUntilStr = await DisplayPromptAsync(
                "Nowa grupa",
                "Data wygaśnięcia grupy (opcjonalnie, format: DD.MM.RRRR):",
                accept: "Utwórz",
                cancel: "Anuluj",
                placeholder: DateTime.Today.AddMonths(1).ToString("dd.MM.yyyy"),
                keyboard: Keyboard.Numeric);

            if (validUntilStr == null) // użytkownik nacisnął Anuluj
                return;

            DateTime? validUntil = null;
            if (!string.IsNullOrWhiteSpace(validUntilStr))
            {
                if (DateTime.TryParseExact(validUntilStr.Trim()+" 23:59:59", "dd.MM.yyyy HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsed))
                {
                    validUntil = parsed;
                }
                else
                {
                    await DisplayAlert("Błąd", "Nieprawidłowy format daty. Użyj formatu DD.MM.RRRR.", "OK");
                    return;
                }
            }

            IsBusy = true;

            var api = new SingingGroupApi(_settings.ApiBaseUrl);
            var created = await api.CreateGroupAsync(new SingingGroup
            {
                GroupName = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                Creator = _userViewModel?.UserEmail,
                ValidUntil = validUntil
            });

            // Zapisz nową grupę lokalnie
            if (_repo != null)
            {
                var entity = new SingingGroupEntity
                {
                    Id = created.Id,
                    GroupName = created.GroupName,
                    Description = created.Description,
                    Leader = created.Leader,
                    LastSetDate = created.LastSetDate,
                    IsLocalOnly = false
                };
                await _repo.UpsertAsync(entity);
                Groups.Insert(0, entity);
                OnPropertyChanged(nameof(IsEmptyGroups));
            }

            await DisplayAlert("Sukces", $"Grupa „{created.GroupName}” została utworzona.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Nie udało się utworzyć grupy: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            _duringGroupCreation = false;
        }
    }

    /// <summary>
    /// Wybrano grupę - zapytaj o rolę i zarejestruj wybór. Obsłuż błędy i pokaż komunikaty użytkownikowi.
    /// </summary>
    private async void OnGroupTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not SingingGroupEntity entity) return;

            var options = new List<string>
            {
                "🎼 Dyrygent – wybieram piosenki dla grupy",
                "🎵 Artysta – odbieram sygnały o zmianie piosenki"
            };

            if (entity.IsSelected == true)
                options.Add("🚫 Opuść grupę");

            string? choice = await DisplayActionSheet(
                $"Dołącz do grupy: {entity.GroupName}",
                "Anuluj",
                null,
                options.ToArray());

            if (string.IsNullOrEmpty(choice) || choice == "Anuluj")
                return;

            var role = choice.StartsWith("🎼") ? SingingGroupRole.Dyrygent : SingingGroupRole.Artysta;

            foreach (var g in Groups)
            {
                g.IsSelected = false;
                g.SelectedRole = SingingGroupRole.None;
                if (_repo != null)
                    await _repo.UpsertAsync(g);
            }

            if (choice.StartsWith("🚫"))
            {
                entity.IsSelected = false;
                entity.SelectedRole = SingingGroupRole.None;
                if (_repo != null)
                    await _repo.UpsertAsync(entity);
                OnPropertyChanged(nameof(IsEmptyGroups));
                AmIDirector = false;
                return;
            }

            entity.IsSelected = true;
            entity.SelectedRole = role;

            if (_repo != null)
                await _repo.UpsertAsync(entity);

            if (role == SingingGroupRole.Dyrygent && !entity.IsLocalOnly)
                await RegisterAsDyrygentAsync(entity);

            if (_listenersGroupBroadcastService != null)
            {
                if (_listenersGroupBroadcastService?.BtPermissionsDecision != PermissionsDecision.Granted)
                    await _listenersGroupBroadcastService!.RunBluetoothPollingAsync(requeryForPermissions: true);

                _listenersGroupBroadcastService.StartGroupPolling();
            }

            AmIDirector = (role == SingingGroupRole.Dyrygent);

            OnPropertyChanged(nameof(IsEmptyGroups));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnGroupTapped error: {ex.Message}");
        }
    }

    /// <summary>
    /// Rejestruje użytkownika jako lidera (Dyrygenta) grupy w API.
    /// </summary>
    private async Task RegisterAsDyrygentAsync(SingingGroupEntity entity)
    {
        try
        {
            if (_settings == null || _userViewModel == null) return;

            var email = _userViewModel.UserEmail;
            var guid  = _userViewModel.AppGuid;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(guid))
            {
                await DisplayAlert("Uwaga", "Musisz być zalogowany, żeby pełnić rolę Dyrygenta.", "OK");
                return;
            }

            try
            {
                var api = new SingingGroupApi(_settings.ApiBaseUrl);
                await api.ChangeLeaderAsync(entity.Id, email, guid);
            }
            catch { }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Nie udało się zarejestrować jako Dyrygent: {ex.Message}", "OK");
        }
    }

    public bool IsEmptyGroups => Groups == null || Groups.Count == 0;
}