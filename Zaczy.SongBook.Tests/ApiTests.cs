using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;

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


}
