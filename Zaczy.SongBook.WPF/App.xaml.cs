using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Enums;

namespace Zaczy.SongBook.WPF;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                services.Configure<AppSettings>(configuration);

                var conn = configuration.GetSection("ConnectionStrings")["SongBookDb"];
                var providerString = configuration.GetSection("Settings")["DbProvider"] ?? "MySql";
                var provider = Enum.TryParse<SongBookDbProvider>(providerString, ignoreCase: true, out var p)
                    ? p
                    : SongBookDbProvider.MySql;

                if (!string.IsNullOrEmpty(conn))
                {
                    services.AddDbContext<SongBookDbContext>(options =>
                    {
                        switch (provider)
                        {
                            case SongBookDbProvider.Sqlite:
                                options.UseSqlite(conn);
                                break;
                            case SongBookDbProvider.MySql:
                            default:
                                options.UseMySql(conn, ServerVersion.AutoDetect(conn));
                                break;
                        }
                    });
                }

                services.AddSingleton<ViewModel>();
                services.AddTransient<SongRepository>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SongBookDbContext>();
            db.Database.EnsureCreated();
        }

        var main = _host.Services.GetRequiredService<MainWindow>();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
