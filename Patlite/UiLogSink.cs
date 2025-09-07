using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Patlite;
    public sealed class UiLogSink : ILogEventSink, IDisposable
    {
        private readonly MainVM _vm;
        private readonly MessageTemplateTextFormatter _formatter;

        public UiLogSink(MainVM vm, string? template = null)
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

        public void Dispose() { }
    }
