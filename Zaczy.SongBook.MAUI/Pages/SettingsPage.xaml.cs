using Microsoft.Maui.Controls;
using Zaczy.SongBook.MAUI.ViewModels;
using System;
using Zaczy.SongBook.Enums;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly UserViewModel _userViewModel;

    public SettingsPage(UserViewModel userViewModel)
    {
        InitializeComponent();

        _userViewModel = userViewModel ?? throw new ArgumentNullException(nameof(userViewModel));
        BindingContext = _userViewModel;

        // initialize AutoScrollEntry text from ViewModel
        //AutoScrollEntry.Text = _userViewModel.AutoScrollSpeed?.ToString() ?? string.Empty;

        // initialize radio buttons from current preference
        RadioPre.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.Pre;
        RadioRelative.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.RelativeHtml;
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

    //private void OnAutoScrollSpeedTextChanged(object sender, TextChangedEventArgs e)
    //{
    //    if (string.IsNullOrWhiteSpace(e.NewTextValue))
    //    {
    //        _userViewModel.AutoScrollSpeed = null;
    //        return;
    //    }

    //    if (int.TryParse(e.NewTextValue, out var val))
    //    {
    //        _userViewModel.AutoScrollSpeed = val;
    //    }
    //    else
    //    {
    //        // revert invalid input
    //        AutoScrollEntry.Text = _userViewModel.AutoScrollSpeed?.ToString() ?? string.Empty;
    //    }
    //}

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        _userViewModel.FontSizeAdjustment = 0;
        _userViewModel.AutoScrollSpeed = null;

        // reset radios
        RadioPre.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.Pre;
        RadioRelative.IsChecked = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.RelativeHtml;
    }
}