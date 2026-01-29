using LiteDB;
using MauiIcons.Fluent;
using MauiIcons.FontAwesome.Solid;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System.Collections.Generic;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.Pages;
using Zaczy.SongBook.MAUI.Spotify;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseFontAwesomeSolidMauiIcons()
                .UseFluentMauiIcons()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Try regular file first (useful on Windows)
            try
            {
                builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            }
            catch
            {
                // ignore; fallback below will try stream load
            }

            //builder.Services.AddSingleton<ISpotifyRemoteService, SpotifyRemoteService>();


            try
            {
                var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
                if (stream != null)
                {
                    using (stream)
                    {
                        builder.Configuration.AddJsonStream(stream);
                    }
                }
            }
            catch
            {
                // ignore - if the packaged file isn't available we continue with default/empty config
            }

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // register settings POCO from "Settings" section
            builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

            // If running on Android emulator, replace host alias with emulator-accessible IP
            if (OperatingSystem.IsAndroid())
            {
                var apiBase = builder.Configuration.GetValue<string>("Settings:ApiBaseUrl");
                if (!string.IsNullOrEmpty(apiBase) && apiBase.Contains("zaczy-api.local"))
                {
                    apiBase = apiBase.Replace("zaczy-api.local", "10.0.2.2");
                    var dict = new Dictionary<string, string>
                    {
                        ["Settings:ApiBaseUrl"] = apiBase
                    };

                    builder.Configuration.AddInMemoryCollection(dict!);
                }
            }

            // Configure LiteDB (single-file, embedded, cross-platform)
            var appData = FileSystem.AppDataDirectory; // MAUI app data folder
            Directory.CreateDirectory(appData);
            var dbPath = Path.Combine(appData, "songbook_lite.db");

            // Create or open LiteDB database
            // Use connection string if you want options: e.g. "Filename=...; Connection=shared"
            var liteDb = new LiteDatabase(dbPath);

            // Register LiteDatabase and repository
            builder.Services.AddSingleton<LiteDatabase>(liteDb);
            builder.Services.AddSingleton<SongRepositoryLite>();

            // Register ViewModels and Pages in DI
            builder.Services.AddTransient<SongListViewModel>();
            builder.Services.AddSingleton<UserViewModel>();

            builder.Services.AddTransient<SongsPage>();
            builder.Services.AddTransient<SongDetailsPage>();
            builder.Services.AddTransient<SettingsPage>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Seed initial data if needed
            using (var scope = app.Services.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<SongRepositoryLite>();
                repo.SeedIfEmpty();
            }

            return app;
        }
    }
}
