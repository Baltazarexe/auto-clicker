using System.Windows;
using System.Windows.Threading;
using AutoClicker.Core;
using AutoClicker.Native;
using AutoClicker.UI;

namespace AutoClicker;

public partial class App : Application
{
    private ClickEngine? _engine;
    private GlobalInputHook? _hook;
    private DispatcherTimer? _timer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, _) => ShutdownSafely();
        TimerResolution.KeepHighResolutionWhenHidden();

        var store = new SettingsStore(SettingsStore.DefaultPath);
        _engine = new ClickEngine(new SendInputClickSender());
        var engine = _engine;
        var controller = new ClickController(engine, store.Load());
        var vm = new MainViewModel(controller, store, () => engine.RealCps, a => Dispatcher.BeginInvoke(a));
        engine.RunningChanged += _ => Dispatcher.BeginInvoke(vm.Refresh);

        _hook = new GlobalInputHook(controller.OnHotkey);
        _hook.Start();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => { if (engine.IsRunning) vm.Refresh(); };
        _timer.Start();

        var window = new MainWindow { DataContext = vm };
        window.Closing += (_, _) => ShutdownSafely();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ShutdownSafely();
        base.OnExit(e);
    }

    /// <summary>Stops clicking and removes the hooks. Safe to call more than once.</summary>
    private void ShutdownSafely()
    {
        // Remove the hooks first (so no F6 can restart the engine), then stop the engine.
        _timer?.Stop();
        _hook?.Dispose();
        _hook = null;
        _engine?.Stop();
    }
}
