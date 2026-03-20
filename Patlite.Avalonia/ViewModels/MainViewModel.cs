using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Patlite.lib;

namespace Patlite.Avalonia.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IPNS _pns;

    public MainViewModel(ILogger<MainViewModel> logger, IPNS pns)
    {
        _logger = logger;
        _pns = pns;

        La6Colours = Enum.GetValues<La6Colour>();
        FlashValues = Enum.GetValues<Flash>();
        BuzzerValues = Enum.GetValues<BuzzerPattern>();

        _tier1 = La6Colour.Red;
        _tier2 = _tier3 = _tier4 = _tier5 = La6Colour.Off;
        _flash = Flash.Off;
        _buzzer = BuzzerPattern.Off;

        _appliedTier1 = _appliedTier2 = _appliedTier3 = _appliedTier4 = _appliedTier5 = La6Colour.Off;
        _appliedFlash = Flash.Off;
        _appliedBuzzer = BuzzerPattern.Off;

        SendCommand = new AsyncRelayCommand(SendAsync, _ => !IsBusy && IsEndpointValid());
        OffCommand = new AsyncRelayCommand(OffAsync, _ => !IsBusy && IsEndpointValid());
        ApplyAndSendPresetCommand = new AsyncRelayCommand(ApplyAndSendPresetAsync, _ => !IsBusy && IsEndpointValid());

        Presets.Add(LightPreset.From("Free", La6Colour.Green, La6Colour.Off, La6Colour.Off, La6Colour.Off, La6Colour.Off, Flash.Off, BuzzerPattern.Off));
        Presets.Add(LightPreset.From("Concentrating", La6Colour.Amber, La6Colour.Amber, La6Colour.Amber, La6Colour.White, La6Colour.White, Flash.Off, BuzzerPattern.Off));
        Presets.Add(LightPreset.From("Busy", La6Colour.Red, La6Colour.Red, La6Colour.Red, La6Colour.White, La6Colour.White, Flash.Off, BuzzerPattern.Off));
        Presets.Add(LightPreset.From("Normal", La6Colour.Green, La6Colour.Purple, La6Colour.Blue, La6Colour.Pink, La6Colour.Lemon, Flash.Off, BuzzerPattern.Off));
        Presets.Add(LightPreset.From("Fire", La6Colour.Red, La6Colour.Red, La6Colour.Red, La6Colour.Red, La6Colour.Red, Flash.On, BuzzerPattern.Off));
    }

    public ObservableCollection<LightPreset> Presets { get; } = new();
    public ObservableCollection<string> LogLines { get; } = new();
    public ICommand SendCommand { get; }
    public ICommand OffCommand { get; }
    public ICommand ApplyAndSendPresetCommand { get; }

    public IReadOnlyList<La6Colour> La6Colours { get; }
    public IReadOnlyList<Flash> FlashValues { get; }
    public IReadOnlyList<BuzzerPattern> BuzzerValues { get; }

    private string _ip = string.Empty;
    public string Ip
    {
        get => _ip;
        set
        {
            if (Set(ref _ip, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private string _port = string.Empty;
    public string Port
    {
        get => _port;
        set
        {
            if (Set(ref _port, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private La6Colour _tier1;
    public La6Colour Tier1
    {
        get => _tier1;
        set
        {
            if (Set(ref _tier1, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private La6Colour _tier2;
    public La6Colour Tier2
    {
        get => _tier2;
        set
        {
            if (Set(ref _tier2, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private La6Colour _tier3;
    public La6Colour Tier3
    {
        get => _tier3;
        set
        {
            if (Set(ref _tier3, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private La6Colour _tier4;
    public La6Colour Tier4
    {
        get => _tier4;
        set
        {
            if (Set(ref _tier4, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private La6Colour _tier5;
    public La6Colour Tier5
    {
        get => _tier5;
        set
        {
            if (Set(ref _tier5, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private Flash _flash;
    public Flash Flash
    {
        get => _flash;
        set
        {
            if (Set(ref _flash, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private BuzzerPattern _buzzer;
    public BuzzerPattern Buzzer
    {
        get => _buzzer;
        set
        {
            if (Set(ref _buzzer, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    private La6Colour _appliedTier1;
    public La6Colour AppliedTier1
    {
        get => _appliedTier1;
        private set => Set(ref _appliedTier1, value);
    }

    private La6Colour _appliedTier2;
    public La6Colour AppliedTier2
    {
        get => _appliedTier2;
        private set => Set(ref _appliedTier2, value);
    }

    private La6Colour _appliedTier3;
    public La6Colour AppliedTier3
    {
        get => _appliedTier3;
        private set => Set(ref _appliedTier3, value);
    }

    private La6Colour _appliedTier4;
    public La6Colour AppliedTier4
    {
        get => _appliedTier4;
        private set => Set(ref _appliedTier4, value);
    }

    private La6Colour _appliedTier5;
    public La6Colour AppliedTier5
    {
        get => _appliedTier5;
        private set => Set(ref _appliedTier5, value);
    }

    private Flash _appliedFlash;
    public Flash AppliedFlash
    {
        get => _appliedFlash;
        private set => Set(ref _appliedFlash, value);
    }

    private BuzzerPattern _appliedBuzzer;
    public BuzzerPattern AppliedBuzzer
    {
        get => _appliedBuzzer;
        private set => Set(ref _appliedBuzzer, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool TryParsePort(out int port)
        => int.TryParse(Port, out port) && port is >= 1 and <= 65535;

    private bool IsEndpointValid()
        => !string.IsNullOrWhiteSpace(Ip) && TryParsePort(out _);

    private async Task SendAsync(object? _)
    {
        if (!TryParsePort(out var port))
        {
            return;
        }

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
            if (ok)
            {
                _logger.LogInformation("Pattern sent.");
                ApplyPatternToApplied(pattern);
            }
            else
            {
                _logger.LogWarning("Failed to send pattern.");
            }
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
        {
            return;
        }

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
            else
            {
                _logger.LogWarning("Failed to send Off.");
            }
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

    private async Task ApplyAndSendPresetAsync(object? parameter)
    {
        if (parameter is not LightPreset preset)
        {
            return;
        }

        if (!TryParsePort(out var port) || string.IsNullOrWhiteSpace(Ip))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var cfg = new PatliteConfig { ip = Ip, port = port };
            var pattern = new PatlitePattern
            {
                Tier1 = preset.Tier1,
                Tier2 = preset.Tier2,
                Tier3 = preset.Tier3,
                Tier4 = preset.Tier4,
                Tier5 = preset.Tier5,
                Flash = preset.Flash,
                Buzzer = preset.Buzzer
            };

            var ok = await _pns.SendPnsAsync(cfg, pattern, CancellationToken.None);
            if (ok)
            {
                _logger.LogInformation("Preset '{Name}' sent.", preset.Name);
                ApplyPatternToApplied(pattern);
            }
            else
            {
                _logger.LogWarning("Failed to send preset '{Name}'.", preset.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending preset '{Name}'", preset.Name);
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecuteChanged();
        }
    }

    private void RaiseCanExecuteChanged()
    {
        (SendCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OffCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ApplyAndSendPresetCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    public void AddUiLogLine(string line)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LogLines.Add(line);
            if (LogLines.Count > 200)
            {
                LogLines.RemoveAt(0);
            }
        });
    }

    private void ApplyPatternToApplied(PatlitePattern pattern)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AppliedTier1 = pattern.Tier1;
            AppliedTier2 = pattern.Tier2;
            AppliedTier3 = pattern.Tier3;
            AppliedTier4 = pattern.Tier4;
            AppliedTier5 = pattern.Tier5;
            AppliedFlash = pattern.Flash;
            AppliedBuzzer = pattern.Buzzer;
        });
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        if (name is not null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        return true;
    }
}
