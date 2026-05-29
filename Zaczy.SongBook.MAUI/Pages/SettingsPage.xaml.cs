using Microsoft.Maui.Controls;
using Zaczy.SongBook.MAUI.ViewModels;
using System;
using Zaczy.SongBook.Enums;
using Zaczy.SongBook;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly UserViewModel _userViewModel;

    public SettingsPage(UserViewModel userViewModel)
    {
        InitializeComponent();

        _userViewModel = userViewModel ?? throw new ArgumentNullException(nameof(userViewModel));
        BindingContext = _userViewModel;

        RadioPre.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.Pre;
        RadioRelative.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.RelativeHtml;

        RadioGuitar.IsChecked = _userViewModel.ChordsInstrument == InstrumentType.Guitar;
        RadioUkulele.IsChecked = _userViewModel.ChordsInstrument == InstrumentType.Ukulele;
    }

    private void OnLyricsVersionRadioChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value || sender is not RadioButton rb)
            return;

        if (rb == RadioPre)
            _userViewModel.LyricsHtmlVersion = LyricsHtmlVersion.Pre;
        else if (rb == RadioRelative)
            _userViewModel.LyricsHtmlVersion = LyricsHtmlVersion.RelativeHtml;
    }

    private void OnChordsInstrumentRadioChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value || sender is not RadioButton rb)
            return;

        if (rb == RadioGuitar)
            _userViewModel.ChordsInstrument = InstrumentType.Guitar;
        else if (rb == RadioUkulele)
            _userViewModel.ChordsInstrument = InstrumentType.Ukulele;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        _userViewModel.FontSizeAdjustment = 0;
        _userViewModel.AutoScrollSpeed = null;

        RadioPre.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.Pre;
        RadioRelative.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.RelativeHtml;

        RadioGuitar.IsChecked = _userViewModel.ChordsInstrument == InstrumentType.Guitar;
        RadioUkulele.IsChecked = _userViewModel.ChordsInstrument == InstrumentType.Ukulele;
    }
}