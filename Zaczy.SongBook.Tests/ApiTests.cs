using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Models;

namespace Zaczy.SongBook.Tests;

public class ApiTests
{
    private const string ApiBaseUrl = "http://api.zaczy.net/api";
    private const string ApiBaseUrlLocal = "http://zaczy-api.local/api";

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task GetAllCategories_ReturnsCategories()
    {
        // Arrange
        var songApi = new SongApi(ApiBaseUrl);

        // Act
        var response = await songApi.GetCategoriesListAsync(String.Empty);

        if (response != null)
        {
            foreach (var category in response)
            {
                Console.WriteLine($"{category.Id} {category.Name} (piosenek: {category.SongsCount})");
            }
        }

        // Assert
        Assert.That(response?.Count() > 0, "API returned empty song list");
    }

    [Test]
    public async Task GetCategoriesSong_ReturnsCategoryWithSongs()
    {
        // Arrange
        var songApi = new SongApi(ApiBaseUrl);
        int testCategoryId = 1; // Use a valid category ID for testing

        // Act
        var response = await songApi.GetCategorySongsAsync(testCategoryId);

        if (response != null)
        {
            Console.WriteLine($"Kategoria: {response.Name} (piosenek: {response.SongsCount})");
            if (response.Songs != null)
            {
                foreach (var song in response.Songs)
                {
                    Console.WriteLine($"--- {song.Title}");
                }
            }
        }

        // Assert
        Assert.That(response != null, "API returned empty song list");
    }

    [Test]
    public async Task User_CreateOrUpdateUserAsync_ReturnsSuccess()
    {
        // Arrange
        var userApi = new UserApi(ApiBaseUrl);

        // When
        await userApi.CreateOrUpdateUserAsync("zaczy@zaczy.net", "test-token", "https://example.com/picture.jpg");

        // Assert
        Assert.That(true);

    }

    [Test]
    public async Task User_GetByToken_ReturnsAdminUser()
    {
        // Arrange
        var userApi = new UserApi(ApiBaseUrl);
        string token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjI1MDdmNTFhZjJhMTYyNDY3MDc0ODQ2NzRhNDJhZTNjMmI2MjMxOWMiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20iLCJhenAiOiI4NDMzMTY1MTcxMy1mY3JmYm9ianQzMHQxanQ2cmx1NHZnN2VlOTg4c2VwMi5hcHBzLmdvb2dsZXVzZXJjb250ZW50LmNvbSIsImF1ZCI6Ijg0MzMxNjUxNzEzLWZjcmZib2JqdDMwdDFqdDZybHU0dmc3ZWU5ODhzZXAyLmFwcHMuZ29vZ2xldXNlcmNvbnRlbnQuY29tIiwic3ViIjoiMTEyNDczMzM5MzYwMTYxMjQxOTY0IiwiZW1haWwiOiJ6YWN6eS5uZXRAZ21haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsImF0X2hhc2giOiJudVB1RUJtbDRXYnpjTTdpbkRVdFFRIiwibmFtZSI6IlJhZmFsIFphayIsInBpY3R1cmUiOiJodHRwczovL2xoMy5nb29nbGV1c2VyY29udGVudC5jb20vYS9BQ2c4b2NKRHlCa1JreHNZQmtvQmF0RlIzYVd6OFlkdzlLaVRKV1JrOWNuM051RFVoV3Z1QU5Yej1zOTYtYyIsImdpdmVuX25hbWUiOiJSYWZhbCIsImZhbWlseV9uYW1lIjoiWmFrIiwiaWF0IjoxNzcyNTQ0ODI1LCJleHAiOjE3NzI1NDg0MjV9.Q1GHGotK9Dyw-RFsnJxzTbPK67QBU39UiS4EKGvnRWEXdKIcnsfG-qHuferWHFdFWSM329HOrweZz-FxmOwcyTGE_yIQce4Hi1zLYFxXRuVlBgGVSlyWV1iohonr0Bw9xBERVXuVMHcb1ZEmDZkTJI6DvkKabjFJkFZ_6nVj_YSoQl8vJNnVXZDidHEyJKFscje3zZNEfq9UKXBdEtMxZXgu3TUamMTa_hSXMiOXAahT8-lA_mplnvePR3UOP1J4zfPNMRkpiRucJjHCBuHTabA41tbR_blZYEON777IsvaM2nd8QzqPHhp1mWeRJpFHbKkzbgYoA46pNV90Jpkxdw";
        // When
        var user = await userApi.GetUserByTokenAsync(token);

        // Assert
        Assert.That(user?.IsAdmin == true);

    }

    [Test]
    public async Task SingingGroups_CreateGroup_ReturnsGroup()
    {
        // Arrange
        var singingGroupApi = new SingingGroupApi(ApiBaseUrl);
        // When
        var group = await singingGroupApi.CreateGroupAsync(new SingingGroup
        {
            GroupName = "Test",
            Description = "Grupa testowa do testowania API"
        });

        // Assert
        Assert.That(group?.Id > 0);

    }

    [Test]
    public async Task SingingGroups_GetList_ReturnsList()
    {
        // Arrange
        var singingGroupApi = new SingingGroupApi(ApiBaseUrl);
        // When
        var groups = await singingGroupApi.GetServerGroupsAsync();

        if(groups != null)
        {
            foreach (var group in groups)
            {
                Console.WriteLine($"{group.Id} {group.GroupName}");
            }
        }   

        // Assert
        Assert.That(groups?.Count > 0);

    }

    [Test]
    [TestCase(1, 1)]
    [TestCase(2, 12)]
    [TestCase(3, 5)]
    public async Task SingingGroups_SetSong_ReturnsList(int groupId, int songId)
    {
        // Arrange
        var singingGroupApi = new SingingGroupApi(ApiBaseUrl);
        // When
        var group = await singingGroupApi.ChangeSongAsync(groupId, songId, "D");

        if (group != null)
        {
            Console.WriteLine($"{group.Id} {group.GroupName} - song: {group.CurrentSongId}");
        }

        // Assert
        Assert.That(group?.Id > 0);

    }

    [Test]
    [TestCase(1, "")]
    [TestCase(2, "rafal.zak@zaczy.net")]
    [TestCase(3, "zaczy.net@gmail.com")]
    public async Task SingingGroups_SetLeader_ReturnsList(int groupId, string userEmail)
    {
        // Arrange
        var singingGroupApi = new SingingGroupApi(ApiBaseUrl);
        // When
        var group = await singingGroupApi.ChangeLeaderAsync(groupId, userEmail, Guid.NewGuid().ToString());

        if (group != null)
        {
            Console.WriteLine($"{group.Id} {group.GroupName} - leader: {group.Leader}");
        }

        // Assert
        Assert.That(group?.Id > 0);

    }


    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public async Task SingingGroups_GetStatus_ReturnsStatus(int id)
    {
        // Arrange
        var singingGroupApi = new SingingGroupApi(ApiBaseUrl);
        // When
        var status = await singingGroupApi.GetServerGroupsStatusAsync(groupId: id);

        if (status != null)
        {
            Console.WriteLine($"{status.Id} {status.GroupName} - leader {status.Leader} {status.LeaderGuid}, song: {status.CurrentSongId} {status.CurrentSong?.Title}");
        }

        // Assert
        Assert.That(status?.Id >0);

    }



}
