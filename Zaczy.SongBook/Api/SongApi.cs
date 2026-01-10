using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.Api;

public class SongApi
{
    string _baseUrl;

    public SongApi(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public async Task SyncApi(SongRepository songRepository)
    {
        var apiClient = new ApiClient(_baseUrl);

        var songs = await songRepository.GetAllAsync();

        var request = new SongsAllRequest() { Songs = songs };

        var response = await apiClient.PostAsync("/songs/sync", request);
    }
}
