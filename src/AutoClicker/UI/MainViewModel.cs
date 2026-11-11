using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AutoClicker.Core;

namespace AutoClicker.UI;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private readonly ClickController _controller;
    private readonly SettingsStore _store;
    private readonly Func<int> _realCps;
    private string _cpsText;

    /// <param name="post">Runs an action on the UI thread (in the app: Dispatcher.BeginInvoke).</param>
    public MainViewModel(ClickController controller, SettingsStore store, Func<int> realCps, Action<Action> post)
    {
        _controller = controller;
        _store = store;
        _realCps = realCps;
        _cpsText = controller.Settings.Cps.ToString(CultureInfo.InvariantCulture);

        controller.StateChanged += () => post(Refresh);
        controller.Captured += _ => post(() => { _store.Save(_controller.Settings); Refresh(); });

        PrimaryCommand = new RelayCommand(() => { CommitCps(); _controller.ToggleFromButton(); });
        CaptureHotkeyCommand = new RelayCommand(() =>
        {
            if (_controller.IsCapturing) _controller.CancelCapture();
            else _controller.BeginCapture();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand PrimaryCommand { get; }
    public ICommand CaptureHotkeyCommand { get; }

    public string CpsText
    {
        get => _cpsText;
        set
        {
            if (_cpsText == value) return;
            _cpsText = value;
            Raise();
            // Applies immediately (e.g. typed 5000 and pressed F6 without leaving the field).
            // The text is only rewritten/normalized in CommitCps.
            if (long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                int cps = AppSettings.ClampCps(parsed);
                if (cps != _controller.Settings.Cps) Apply(_controller.Settings with { Cps = cps });
            }
        }
    }

    public ClickButton Button
    {
        get => _controller.Settings.Button;
        set { if (value != Button) Apply(_controller.Settings with { Button = value }); }
    }

    public ClickMode Mode
    {
        get => _controller.Settings.Mode;
        set { if (value != Mode) Apply(_controller.Settings with { Mode = value }); }
    }

    public bool IsRunning => _controller.IsRunning;
    public bool IsCapturing => _controller.IsCapturing;
    public bool PrimaryEnabled => Mode == ClickMode.Toggle;

    public string HotkeyLabel => IsCapturing ? "Press a key…" : Display(_controller.Settings.Hotkey);

    public string PrimaryLabel =>
        Mode == ClickMode.Hold ? $"Hold {Display(_controller.Settings.Hotkey)}"
        : IsRunning ? "Stop" : "Start";

    public string RealCpsLabel => $"{_realCps().ToString("N0", EnUs)} real CPS";

    /// <summary>Validates the CPS text: invalid reverts to the last value; out of range is clamped.</summary>
    public void CommitCps()
    {
        int current = _controller.Settings.Cps;
        if (!long.TryParse(_cpsText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
        {
            CpsText = current.ToString(CultureInfo.InvariantCulture);
            return;
        }
        int cps = AppSettings.ClampCps(parsed);
        CpsText = cps.ToString(CultureInfo.InvariantCulture);
        if (cps != current) Apply(_controller.Settings with { Cps = cps });
    }

    /// <summary>Refreshes everything that depends on engine/controller state.</summary>
    public void Refresh()
    {
        Raise(nameof(IsRunning));
        Raise(nameof(IsCapturing));
        Raise(nameof(Button));
        Raise(nameof(Mode));
        Raise(nameof(PrimaryEnabled));
        Raise(nameof(PrimaryLabel));
        Raise(nameof(HotkeyLabel));
        Raise(nameof(RealCpsLabel));
    }

    private void Apply(AppSettings s)
    {
        _controller.Apply(s);
        _store.Save(s);
        Refresh();
    }

    private static string Display(Hotkey h) => h.Kind == HotkeyKind.Mouse
        ? (h.Code == 1 ? "Mouse 4" : "Mouse 5")
        : h.ToString();

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class RelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
}
