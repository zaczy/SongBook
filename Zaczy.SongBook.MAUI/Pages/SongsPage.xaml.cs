using MauiIcons.Core;
using Microsoft.Maui.Controls;
using System;
using Zaczy.SongBook.Data;
using Zaczy.SongBook;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Pages
{
    public partial class SongsPage : ContentPage
    {
        private readonly SongListViewModel _songListViewModel;
        private readonly UserViewModel _userViewModel;

        // ViewModel is injected from DI
        public SongsPage(SongListViewModel vm, UserViewModel viewModel)
        {
            _ = new MauiIcon() { Icon = MauiIcons.Fluent.FluentIcons.ArrowClockwise20, IconColor = Colors.Green };
            _ = new MauiIcon() { Icon = MauiIcons.FontAwesome.Solid.FontAwesomeSolidIcons.ArrowRotateLeft, IconColor = Colors.Green };

            InitializeComponent();

            _songListViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
            _userViewModel = viewModel;
            BindingContext = _songListViewModel;
        }

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
                await Navigation.PushAsync(new SongDetailsPage(song, _userViewModel));
            }
            else
            {
                // fallback: get BindingContext from parent element (safer in some templates)
                if (sender is Element el && el.BindingContext is SongEntity ctxSong)
                {
                    await Navigation.PushAsync(new SongDetailsPage(ctxSong, _userViewModel));
                }
            }
        }

        private void OnTitleFilterCompleted(object sender, EventArgs e)
        {
            if (BindingContext is SongListViewModel vm && vm.FilterCommand != null && vm.FilterCommand.CanExecute(null))
            {
                vm.FilterCommand.Execute(null);
            }
        }

        private void OnPageFilterCompleted(object sender, EventArgs e)
        {
            if (BindingContext is SongListViewModel vm && vm.PageCommand != null && vm.PageCommand.CanExecute(null))
            {
                vm.PageCommand.Execute(null);
            }
        }
    }
}