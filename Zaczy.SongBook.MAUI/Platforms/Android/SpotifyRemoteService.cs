/*
#if ANDROID
using Spotify.App.Remote.Api;
using Android.App;
using Zaczy.SongBook.MAUI.Spotify;

public class SpotifyRemoteService : ISpotifyRemoteService
{
    SpotifyAppRemote _remote;

    public async Task<bool> ConnectAsync()
    {
        var config = new ConnectionParams.Builder("CLIENT_ID")
            .SetRedirectUri("myapp://callback")
            .ShowAuthView(true)
            .Build();

        SpotifyAppRemote.Connect(
            Application.Context,
            config,
            new ConnectionListener(
                connected: remote => _remote = remote,
                failure: error => _remote = null
            )
        );

        return _remote != null;
    }

    public Task PlayAsync(string spotifyUri)
        => _remote.PlayerApi.Play(spotifyUri);

    public Task PauseAsync()
        => _remote.PlayerApi.Pause();

    public Task ResumeAsync()
        => _remote.PlayerApi.Resume();

    public Task NextAsync()
        => _remote.PlayerApi.SkipNext();

    public Task PreviousAsync()
        => _remote.PlayerApi.SkipPrevious();
}
#endif
*/