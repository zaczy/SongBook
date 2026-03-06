using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MauiIcons.Core;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System;
using Zaczy.SongBook;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Pages
{
    public partial class SongsPage : ContentPage
    {
        private readonly SongListViewModel _songListViewModel;
        private readonly UserViewModel _userViewModel;
        private readonly EventApi _eventApi;
        private readonly Settings _settings;
        private readonly IAudioManager _audioManager;

        /// <summary>
        /// Konstruktor 
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="viewModel"></param>
        /// <param name="eventApi"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SongsPage(SongListViewModel vm, UserViewModel viewModel, EventApi eventApi, IOptions<Settings> settings, IAudioManager audioManager)
        {
            _ = new MauiIcon() { Icon = MauiIcons.Fluent.FluentIcons.ArrowClockwise20, IconColor = Colors.Green };
            _ = new MauiIcon() { Icon = MauiIcons.FontAwesome.Solid.FontAwesomeSolidIcons.ArrowRotateLeft, IconColor = Colors.Green };

            InitializeComponent();

            _songListViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
            _userViewModel = viewModel;
            BindingContext = _songListViewModel;
            _eventApi = eventApi;
            _settings = settings.Value;
            _audioManager = audioManager;

            // register to receive updates
            WeakReferenceMessenger.Default.Register<SongsPage, ValueChangedMessage<SongEntity>>(this, (page, message) =>
            {
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var updatedSong = message.Value;
                        var existing = _songListViewModel.Songs.FirstOrDefault(s => s.Id == updatedSong.Id);
                        if (existing != null)
                        {
                            var idx = _songListViewModel.Songs.IndexOf(existing);
                            if (idx >= 0)
                            {
                                // replace item to force UI refresh
                                _songListViewModel.Songs[idx] = updatedSong;
                            }
                            else
                            {
                                // update properties in place
                                existing.Title = updatedSong.Title;
                                existing.Artist = updatedSong.Artist;
                                existing.Capo = updatedSong.Capo;
                                existing.Lyrics = updatedSong.Lyrics;
                                existing.UpdatedAt = updatedSong.UpdatedAt;
                            }
                        }
                        else
                        {
                            // not present — add and keep ordering
                            _songListViewModel.Songs.Add(updatedSong);
                            var sorted = _songListViewModel.Songs.OrderBy(s => s.Title ?? string.Empty).ToList();
                            _songListViewModel.Songs.Clear();
                            foreach (var s in sorted)
                                _songListViewModel.Songs.Add(s);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SongUpdated handler error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Wykonywane, kiedy strona staje siê widoczna. Jeœli lista piosenek jest pusta, ³aduje je z bazy danych. 
        /// Dziêki temu dane s¹ odœwie¿ane przy ka¿dym wejœciu na stronê, ale tylko jeœli jest to potrzebne (np. po dodaniu nowej piosenki).
        /// Jeœli dane s¹ ju¿ za³adowane, nie wykonuje ponownie operacji ³adowania.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_songListViewModel.Songs.Count == 0)
                await _songListViewModel.LoadSongsAsync();
        }

        // Handler wired from XAML TapGestureRecognizer. CommandParameter is the SongEntity.
        private async void OnItemTapped(object sender, EventArgs e)
        {
            // sender is the TapGestureRecognizer; get CommandParameter
            if (sender is TapGestureRecognizer tg && tg.CommandParameter is SongEntity song)
            {
                // push details page
                await Navigation.PushAsync(new SongDetailsPage(song, _userViewModel, _eventApi, _songListViewModel.Repo, _settings, _audioManager));
            }
            else
            {
                // fallback: get BindingContext from parent element (safer in some templates)
                if (sender is Element el && el.BindingContext is SongEntity ctxSong)
                {
                    await Navigation.PushAsync(new SongDetailsPage(ctxSong, _userViewModel, _eventApi, _songListViewModel.Repo, _settings, _audioManager));
                }
            }
        }

        /// <summary>
        /// Fitrowanie po tytule i odœwie¿enie listy. Command jest w ViewModelu, wiêc sprawdzamy jego dostêpnoœæ i wykonujemy.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTitleFilterCompleted(object sender, EventArgs e)
        {
            if (BindingContext is SongListViewModel vm && vm.FilterCommand != null && vm.FilterCommand.CanExecute(null))
            {
                vm.FilterCommand.Execute(null);
            }
        }

        /// <summary>
        /// Filtrowanie po stronie serwera (paginacja) i odœwie¿enie listy. Command jest w ViewModelu, wiêc sprawdzamy jego dostêpnoœæ i wykonujemy.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPageFilterCompleted(object sender, EventArgs e)
        {
            if (BindingContext is SongListViewModel vm && vm.PageCommand != null && vm.PageCommand.CanExecute(null))
            {
                vm.PageCommand.Execute(null);
            }
        }

        private void OnItemTapped(object sender, TappedEventArgs e)
        {

        }
    }
}