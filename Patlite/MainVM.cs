using Microsoft.Extensions.Logging;
using Patlite.lib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using Serilog.Core;
using System.Windows.Threading;
using System.Windows;
using System.Collections.ObjectModel;

namespace Patlite;
public interface IMainVM
{
    abstract ObservableCollection<string> LogLines { get; }
}

public class MainVM:IMainVM, INotifyPropertyChanged
{
    private readonly ILogger<MainVM> _logger;
    private readonly IPNS _pns;
    private readonly SynchronizationContext _ui;

    public MainVM(ILogger<MainVM> log, IPNS pNS) {
        _logger = log;
        _pns = pNS;
        _ui = SynchronizationContext.Current ?? new SynchronizationContext();

        _tier1 = La6Colour.Red;
        _tier2 = _tier3 = _tier4 = _tier5 = La6Colour.Off;

        _flash = Flash.Off;
        _buzzer = BuzzerPattern.Off;

        _appliedTier1 = _appliedTier2 = _appliedTier3 = _appliedTier4 = _appliedTier5 = La6Colour.Off;
        
        _appliedFlash = Flash.Off;
        _appliedBuzzer = BuzzerPattern.Off;

        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        OffCommand = new AsyncRelayCommand(OffAsync, _ => !IsBusy && IsEndpointValid());

        // Five fixed presets (tweak to taste)
        Presets.Add(LightPreset.From("Free",
            La6Colour.Green, La6Colour.Off, La6Colour.Off, La6Colour.Off, La6Colour.Off,
            Flash.Off, BuzzerPattern.Off));

        Presets.Add(LightPreset.From("Concentrating",
            La6Colour.Amber, La6Colour.Amber, La6Colour.Amber, La6Colour.White, La6Colour.White,
            Flash.Off, BuzzerPattern.Off));

        Presets.Add(LightPreset.From("Busy",
            La6Colour.Red, La6Colour.Red, La6Colour.Red, La6Colour.White, La6Colour.White,
            Flash.Off, BuzzerPattern.Off));

        Presets.Add(LightPreset.From("Normal",
            La6Colour.Green, La6Colour.Purple, La6Colour.Blue, La6Colour.Pink, La6Colour.Lemon,
            Flash.Off, BuzzerPattern.Off));

        Presets.Add(LightPreset.From("Fire",
            La6Colour.Red, La6Colour.Red, La6Colour.Red, La6Colour.Red, La6Colour.Red,
            Flash.On, BuzzerPattern.Off));

        // One-click apply+send
        ApplyAndSendPresetCommand = new AsyncRelayCommand(ApplyAndSendPresetAsync, _ => !IsBusy && IsEndpointValid());

    }

    #region Bindable Properties
    public ObservableCollection<LightPreset> Presets { get; } = new();
    public ICommand ApplyAndSendPresetCommand { get; }

    private string _ip = string.Empty;
    public string Ip
    {
        get => _ip;
        set { if (Set(ref _ip, value)) RaiseCanExecuteChanged(); }
    }

    // Port as string to keep TextBox binding simple (no converter needed)
    private string _port = string.Empty;
    public string Port
    {
        get => _port;
        set { if (Set(ref _port, value)) RaiseCanExecuteChanged(); }
    }

    private La6Colour _tier1, _tier2, _tier3, _tier4, _tier5;
    public La6Colour Tier1 { get => _tier1; set { if (Set(ref _tier1, value)) RaiseCanExecuteChanged(); } }
    public La6Colour Tier2 { get => _tier2; set { if (Set(ref _tier2, value)) RaiseCanExecuteChanged(); } }
    public La6Colour Tier3 { get => _tier3; set { if (Set(ref _tier3, value)) RaiseCanExecuteChanged(); } }
    public La6Colour Tier4 { get => _tier4; set { if (Set(ref _tier4, value)) RaiseCanExecuteChanged(); } }
    public La6Colour Tier5 { get => _tier5; set { if (Set(ref _tier5, value)) RaiseCanExecuteChanged(); } }

    private Flash _flash;
    public Flash Flash
    {
        get => _flash;
        set { if (Set(ref _flash, value)) RaiseCanExecuteChanged(); }
    }

    private BuzzerPattern _buzzer;
    public BuzzerPattern Buzzer
    {
        get => _buzzer;
        set { if (Set(ref _buzzer, value)) RaiseCanExecuteChanged(); }
    }

    // Add these fields + properties
    private La6Colour _appliedTier1, _appliedTier2, _appliedTier3, _appliedTier4, _appliedTier5;
    public La6Colour AppliedTier1 { get => _appliedTier1; private set => Set(ref _appliedTier1, value); }
    public La6Colour AppliedTier2 { get => _appliedTier2; private set => Set(ref _appliedTier2, value); }
    public La6Colour AppliedTier3 { get => _appliedTier3; private set => Set(ref _appliedTier3, value); }
    public La6Colour AppliedTier4 { get => _appliedTier4; private set => Set(ref _appliedTier4, value); }
    public La6Colour AppliedTier5 { get => _appliedTier5; private set => Set(ref _appliedTier5, value); }


    private Flash _appliedFlash;
    public Flash AppliedFlash { get => _appliedFlash; private set => Set(ref _appliedFlash, value); }

    private BuzzerPattern _appliedBuzzer;
    public BuzzerPattern AppliedBuzzer { get => _appliedBuzzer; private set => Set(ref _appliedBuzzer, value); }

    private DateTime _lastAppliedAt;
    public DateTime LastAppliedAt { get => _lastAppliedAt; set => Set(ref _lastAppliedAt, value); }


    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { if (Set(ref _isBusy, value)) RaiseCanExecuteChanged(); }
    }
    public ObservableCollection<string> LogLines { get; } = new();

    #endregion

    #region Commands

    public ICommand SendCommand { get; }
    public ICommand OffCommand { get; }

    private bool CanSend(object? _)
        => !IsBusy && IsEndpointValid();

    private bool IsEndpointValid()
        => !string.IsNullOrWhiteSpace(Ip) && TryParsePort(out _);

    private bool TryParsePort(out int port)
        => int.TryParse(Port, out port) && port >= 1 && port <= 65535;

    private async Task SendAsync(object? _)
    {
        if (!TryParsePort(out var port))
            return;

        IsBusy = true;
        try
        {
            var cfg = new PatliteConfig { ip = Ip, port = port };
            var pattern = new PatlitePattern
            {
                Tier1 = Tier1,
                Tier2 = Tier2,
                Tier3 = Tier3,
                Tier4 = Tier4,
                Tier5 = Tier5,
                Flash = Flash,
                Buzzer = Buzzer
            };

            var ok = await _pns.SendPnsAsync(cfg, pattern, CancellationToken.None);
            if (ok) {
                _logger.LogInformation("Pattern sent.");
                ApplyPatternToApplied(pattern);
            }
            else _logger.LogWarning("Failed to send pattern.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending pattern.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OffAsync(object? _)
    {
        if (!TryParsePort(out var port))
            return;

        IsBusy = true;
        try
        {
            var cfg = new PatliteConfig { ip = Ip, port = port };
            var off = new PatlitePattern
            {
                Tier1 = La6Colour.Off,
                Tier2 = La6Colour.Off,
                Tier3 = La6Colour.Off,
                Tier4 = La6Colour.Off,
                Tier5 = La6Colour.Off,
                Flash = Flash.Off,
                Buzzer = BuzzerPattern.Off
            };

            var ok = await _pns.SendPnsAsync(cfg, off, CancellationToken.None);
            if (ok)
            {
                _logger.LogInformation("Off sent.");
                ApplyPatternToApplied(off);
            }
            else _logger.LogWarning("Failed to send Off.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending Off.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCanExecuteChanged()
    {
        (SendCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OffCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ApplyAndSendPresetCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    internal void AddUiLogLine(string line)
    {
        // marshal to UI thread
        var disp = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (!disp.CheckAccess())
            disp.Invoke(() => LogLines.Add(line));
        else
            LogLines.Add(line);

        // keep last 200 lines
        if (LogLines.Count > 200)
            LogLines.RemoveAt(0);
    }

    // BEFORE (what you likely had)
    private async Task ApplyAndSendPresetAsync(object? parameter)
    {
        if (parameter is not LightPreset p) return;

        if (!int.TryParse(Port, out var port)) return;
        if (string.IsNullOrWhiteSpace(Ip)) return;

        IsBusy = true;
        try
        {
            var cfg = new PatliteConfig { ip = Ip, port = port };
            var pattern = new PatlitePattern
            {
                Tier1 = p.Tier1,
                Tier2 = p.Tier2,
                Tier3 = p.Tier3,
                Tier4 = p.Tier4,
                Tier5 = p.Tier5,
                Flash = p.Flash,
                Buzzer = p.Buzzer
            };

            var ok = await _pns.SendPnsAsync(cfg, pattern, CancellationToken.None);
            if (ok)
            {
                _logger.LogInformation("Preset '{Name}' sent.", p.Name);
                // keep indicators truthful to device:
                ApplyPatternToApplied(pattern);
            }
            else
            {
                _logger.LogWarning("Failed to send preset '{Name}'.", p.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending preset '{Name}'", p.Name);
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecuteChanged();
        }
    }



    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
        {
            // Force a repaint for Applied* even if value didn't change
            if (name is not null && name.StartsWith("Applied", StringComparison.Ordinal))
                OnPropertyChanged(name);
            return false;
        }
        field = value;
        OnPropertyChanged(name!);
        return true;
    }

    private void ApplyPatternToApplied(Patlite.lib.PatlitePattern p)
    {
        // Ensure raises happen on the UI thread
        var disp = Application.Current?.Dispatcher;
        if (disp is not null && !disp.CheckAccess())
        {
            disp.Invoke(() => SetAppliedFrom(p), DispatcherPriority.DataBind);
        }
        else
        {
            SetAppliedFrom(p);
        }
    }
    private void SetAppliedFrom(Patlite.lib.PatlitePattern p)
    {
        // Assign all and ALWAYS notify (thanks to Set<T>’s Applied* fast-path above)
        AppliedTier1 = p.Tier1;
        AppliedTier2 = p.Tier2;
        AppliedTier3 = p.Tier3;
        AppliedTier4 = p.Tier4;
        AppliedTier5 = p.Tier5;
        AppliedFlash = p.Flash;
        AppliedBuzzer = p.Buzzer;
    }



    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    #endregion
}

