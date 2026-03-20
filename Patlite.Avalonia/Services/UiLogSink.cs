using System.IO;
using Patlite.Avalonia.ViewModels;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Patlite.Avalonia.Services;

public sealed class UiLogSink : ILogEventSink, IDisposable
{
    private readonly MainViewModel _vm;
    private readonly MessageTemplateTextFormatter _formatter;

    public UiLogSink(MainViewModel vm, string? template = null)
    {
        _vm = vm;
        _formatter = new MessageTemplateTextFormatter(
            template ?? "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
    }

    public void Emit(LogEvent logEvent)
    {
        using var sw = new StringWriter();
        _formatter.Format(logEvent, sw);
        _vm.AddUiLogLine(sw.ToString().TrimEnd());
    }

    public void Dispose()
    {
    }
}
