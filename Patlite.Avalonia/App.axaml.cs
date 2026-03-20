using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Patlite.Avalonia.Services;
using Patlite.Avalonia.ViewModels;
using Patlite.lib;
using Serilog;

namespace Patlite.Avalonia;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var collection = new ServiceCollection();

            collection.AddSingleton<MainViewModel>();
            collection.AddSingleton<IPNS, PNS>();
            collection.AddSingleton<IPatliteCommandBuilder, PatliteCommandBuilder>();
            collection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));

            _services = collection.BuildServiceProvider();

            var vm = _services.GetRequiredService<MainViewModel>();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(new UiLogSink(vm))
                .CreateLogger();

            var loggerFactory = _services.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddSerilog(Log.Logger, dispose: false);

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };

            desktop.Exit += (_, _) =>
            {
                Log.CloseAndFlush();
                _services?.Dispose();
            };

            Log.Information("Avalonia UI logging initialized.");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
