using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Windows;
using Zaczy.SongBook.Data;

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
                // ensure appsettings.json from output is read
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // bind settings
                services.Configure<AppSettings>(configuration);

                // register DbContext (example)
                var conn = configuration.GetSection("ConnectionStrings")["SongBookDb"];
                if (!string.IsNullOrEmpty(conn))
                {
                    services.AddDbContext<SongBookDbContext>(options =>
                        options.UseMySql(conn, ServerVersion.AutoDetect(conn)));
                }

                // register repositories, viewmodels, windows
                services.AddSingleton<ViewModel>();
                //services.AddSingleton<SongRepository>();
                services.AddTransient<SongRepository>();
                services.AddSingleton<MainWindow>();

                // other services
                // services.AddTransient<IMyService, MyService>();
            })
            .Build();

        await _host.StartAsync();

        // resolve and show MainWindow
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
