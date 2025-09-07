using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Patlite.lib;
using Serilog.Sinks.RichTextBox.Themes;
using Serilog;

namespace Patlite
{
    public partial class App : Application
    {
        public static IHost? HostInstance { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            HostInstance = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {

                    services.AddSingleton<MainVM>();                         // concrete
                    services.AddSingleton<IMainVM>(sp => sp.GetRequiredService<MainVM>());
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<IPNS, PNS>();
                    services.AddSingleton<IPatliteCommandBuilder, PatliteCommandBuilder>();

                    // Logging (uses default providers; add Serilog here if you want)
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        // If using Serilog: builder.ClearProviders(); builder.AddSerilog();
                    });
                })
                .Build();

            // 2) Resolve the SAME MainVM the Window will use
            var vm = HostInstance.Services.GetRequiredService<MainVM>();

            // 3) Create Serilog logger that writes to your UI sink (+ any other sinks you want)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(new UiLogSink(vm)) // <= the tiny sink we added
                                                 // .WriteTo.File("logs\\app-.log", rollingInterval: RollingInterval.Day) // optional
                .CreateLogger();

            // 4) Bridge Serilog into the Microsoft logger pipeline AFTER build
            var loggerFactory = HostInstance.Services.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddSerilog(Log.Logger, dispose: false);
            var mainWindow = HostInstance.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Serilog.Log.Information("UI logging initialized.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Serilog.Log.CloseAndFlush();
            HostInstance?.Dispose();
            base.OnExit(e);
        }
    }
}
