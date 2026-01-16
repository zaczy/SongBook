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
        AutoScrollEntry.Text = _userViewModel.AutoScrollSpeed?.ToString() ?? string.Empty;

        // initialize LyricsVersionPicker selection from ViewModel
        LyricsVersionPicker.SelectedIndex = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.Pre ? 0 : 1;
    }

    /// <summary>
    /// Zmiana kontrolki wielkoœci czcionki
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAutoScrollSpeedTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            _userViewModel.AutoScrollSpeed = null;
            return;
        }

        if (int.TryParse(e.NewTextValue, out var val))
        {
            _userViewModel.AutoScrollSpeed = val;
        }
        else
        {
            // revert invalid input
            AutoScrollEntry.Text = _userViewModel.AutoScrollSpeed?.ToString() ?? string.Empty;
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        // reset to defaults (adjust as you wish)
        _userViewModel.FontSizeAdjustment = 0;
        _userViewModel.AutoScrollSpeed = null;
        AutoScrollEntry.Text = string.Empty;

        // reset LyricsHtmlVersion to default and update UI
        _userViewModel.LyricsHtmlVersion = LyricsHtmlVersion.RelativeHtml;
        LyricsVersionPicker.SelectedIndex = _userViewModel.LyricsHtmlVersion == LyricsHtmlVersion.Pre ? 0 : 1;
    }

    private void OnLyricsVersionChanged(object? sender, EventArgs e)
    {
        if (LyricsVersionPicker.SelectedIndex == -1)
            return;

        switch (LyricsVersionPicker.SelectedIndex)
        {
            case 0:
                _userViewModel.LyricsHtmlVersion = LyricsHtmlVersion.Pre;
                break;
            case 1:
                _userViewModel.LyricsHtmlVersion = LyricsHtmlVersion.RelativeHtml;
                break;
            default:
                break;
        }
    }
}