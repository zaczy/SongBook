using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;

namespace Zaczy.SongBook.Tests;

public class ApiTests
{
    private const string ApiBaseUrl = "http://zaczy-api.local/api";

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
        var response = await songApi.GetCategoriesListAsync();

        if(response != null)
        {
            foreach(var category in response)
            {
                Console.WriteLine($"{category.Id} {category.Name} (piosenek: {category.SongsCount})");
            }
        }

        // Assert
        Assert.That(response?.Count()>0, "API returned empty song list");
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

}
