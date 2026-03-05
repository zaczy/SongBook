using System;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class SongEditPage : ContentPage
{
    private readonly UserViewModel _userViewModel;
    private readonly SongEntity _originalSong; 
    private SongEntity _editingSong;           
    private readonly EventApi _eventApi;
    private readonly SongRepositoryLite _repo;
    public UserViewModel UserViewModel => _userViewModel;

    // XAML bindings expect `Song` on the page — expose the editable clone here
    public SongEntity Song => _editingSong;

    // Inject SongRepositoryLite (registered in DI) so we persist edits to local DB.
    public SongEditPage(SongEntity songEntity, UserViewModel userViewModel, EventApi eventApi, SongRepositoryLite songRepository)
	{
        _userViewModel = userViewModel;
        _originalSong = songEntity ?? new SongEntity();
        _editingSong = CloneSong(_originalSong);

        // ensure a sensible default for editing copy (previous code set ScrollingDelay = 10)
        if (_editingSong.ScrollingDelay == null)
        {
            _editingSong.ScrollingDelay = 10;
        }

        _eventApi = eventApi;
        _repo = songRepository ?? throw new ArgumentNullException(nameof(songRepository));

        InitializeComponent();
	}

    // Create a safe shallow/deep clone used for editing.
    // Manual copy is used instead of JSON serialization to avoid issues with internal setters.
    private static SongEntity CloneSong(SongEntity? source)
    {
        if (source == null)
        {
            return new SongEntity();
        }

        return new SongEntity
        {
            Id = source.Id,
            Title = source.Title,
            Artist = source.Artist,
            LyricsAuthor = source.LyricsAuthor,
            MusicAuthor = source.MusicAuthor,
            Capo = source.Capo,
            Lyrics = source.Lyrics,
            Comments = source.Comments,
            ChordsVariations = source.ChordsVariations,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            ScrollingDelay = source.ScrollingDelay,
            SongDuration = source.SongDuration,
            SpotifyLink = source.SpotifyLink,
            Source = source.Source
            // Do not attempt to set MoreInfo here — its setter is internal in the data assembly.
        };
    }

    // Apply changes from the editing copy back to the original instance before persisting.
    private static void ApplyChangesToOriginal(SongEntity from, SongEntity to)
    {
        // Preserve Id and CreatedAt on the original.
        to.Title = from.Title;
        to.Artist = from.Artist;
        to.LyricsAuthor = from.LyricsAuthor;
        to.MusicAuthor = from.MusicAuthor;
        to.Capo = from.Capo;
        to.Lyrics = from.Lyrics;
        to.Comments = from.Comments;
        to.ChordsVariations = from.ChordsVariations;
        to.UpdatedAt = from.UpdatedAt;
        to.ScrollingDelay = from.ScrollingDelay;
        to.SongDuration = from.SongDuration;
        to.SpotifyLink = from.SpotifyLink;
        to.Source = from.Source;
    }

    // Save copies the editable values back to the original and then persists to the local LiteDB via SongRepositoryLite.
    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            // mark updated time on the editing copy
            _editingSong.UpdatedAt = DateTime.UtcNow;

            // copy back to original instance (so callers who hold reference see changes)
            ApplyChangesToOriginal(_editingSong, _originalSong);

            // Persist using SongRepositoryLite
            if (_originalSong.Id == 0)
            {
                // new entity
                await _repo.AddAsync(_originalSong);
            }
            else
            {
                // existing entity
                await _repo.UpdateAsync(_originalSong);
            }

            // Optionally send analytics/event (non-blocking)
            // _ = _eventApi.SendEventAsync("song_saved", $"Song {_originalSong.Id} saved locally.");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie mo¿na zapisaæ piosenki: {ex.Message}", "OK");
        }
    }

    // Cancel simply discards the editing clone and returns — original remains unchanged.
    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        // no changes applied to _originalSong, just pop the page
        await Navigation.PopAsync();
    }

    // Tapped handler that runs BEFORE focus — disables parent ScrollView and then focuses the Editor.
    // This ensures touch is not swallowed by the ScrollView and the soft keyboard is shown.
    private async void OnLyricsEditorTapped(object? sender, EventArgs e)
    {
        try
        {
            // disable parent ScrollView touch handling so Editor receives gestures
            MainScroll.InputTransparent = true;

            // Small delay lets the InputTransparent take effect on some platforms before focusing.
            await Task.Delay(60);

            // Ensure editor gets focus (this should open the soft keyboard)
            LyricsEditor.Focus();
        }
        catch
        {
            // non-critical
        }
    }

    // Called when the lyrics editor receives focus (fallback).
    private void OnLyricsEditorFocused(object? sender, FocusEventArgs e)
    {
        try
        {
            // keep parent scroll disabled while editing so user can scroll inside the Editor
            MainScroll.InputTransparent = true;

            // Optionally bring editor into view
            _ = MainScroll.ScrollToAsync(LyricsEditor, ScrollToPosition.Center, true);
        }
        catch
        {
            // swallow - non-critical
        }
    }

    // Restore parent ScrollView behavior when editor loses focus.
    private void OnLyricsEditorUnfocused(object? sender, FocusEventArgs e)
    {
        try
        {
            MainScroll.InputTransparent = false;
        }
        catch
        {
            // swallow - non-critical
        }
    }
}