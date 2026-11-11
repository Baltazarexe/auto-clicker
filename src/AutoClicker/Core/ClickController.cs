namespace AutoClicker.Core;

/// <summary>
/// Toggle/Hold rules and hotkey capture. Called from the hook thread
/// (OnHotkey) and from the UI (Apply, ToggleFromButton, BeginCapture), so everything is under a lock.
/// Events are raised outside the lock.
/// </summary>
public sealed class ClickController(IClickEngine engine, AppSettings initial)
{
    private readonly object _lock = new();
    private AppSettings _settings = initial;
    private bool _hotkeyDown;
    private bool _capturing;
    private Hotkey? _swallowUp;

    public event Action<Hotkey>? Captured;
    public event Action? StateChanged;

    public AppSettings Settings { get { lock (_lock) return _settings; } }
    public bool IsCapturing { get { lock (_lock) return _capturing; } }
    public bool IsRunning => engine.IsRunning;

    public void Apply(AppSettings s)
    {
        lock (_lock)
        {
            var old = _settings;
            _settings = s;
            if (old.Mode != s.Mode || old.Hotkey != s.Hotkey)
            {
                _hotkeyDown = false;
                engine.Stop();
            }
            else if (engine.IsRunning && (old.Cps != s.Cps || old.Button != s.Button))
            {
                engine.Update(s.Cps, s.Button);
            }
        }
        StateChanged?.Invoke();
    }

    public void ToggleFromButton()
    {
        lock (_lock)
        {
            if (_settings.Mode == ClickMode.Hold || _capturing) return;
            if (engine.IsRunning) engine.Stop();
            else engine.Start(_settings.Cps, _settings.Button);
        }
        StateChanged?.Invoke();
    }

    public void BeginCapture()
    {
        lock (_lock)
        {
            _capturing = true;
            _hotkeyDown = false;
            engine.Stop();
        }
        StateChanged?.Invoke();
    }

    public void CancelCapture()
    {
        lock (_lock) _capturing = false;
        StateChanged?.Invoke();
    }

    /// <summary>Receives every key / XButton. Returns true if the event should be swallowed.</summary>
    public bool OnHotkey(Hotkey key, bool isDown)
    {
        Hotkey? captured = null;
        bool changed = false;
        bool swallow;

        lock (_lock)
        {
            if (_capturing)
            {
                if (!isDown) return false;
                // Typing keys/modifiers are ignored (and swallowed) during capture:
                // as a hotkey they would be blocked system-wide (e.g. WASD in Minecraft).
                if (key != Hotkey.Escape && !key.IsAllowedAsHotkey) return true;
                _capturing = false;
                changed = true;
                if (key != Hotkey.Escape)
                {
                    _settings = _settings with { Hotkey = key };
                    captured = key;
                }
                _swallowUp = key;
                swallow = true;
            }
            else if (_swallowUp == key)
            {
                // The key used for capture is still held down: ignore its auto-repeat and
                // key-up, so the clicker does not start by accident.
                if (!isDown) _swallowUp = null;
                return true;
            }
            else if (key != _settings.Hotkey)
            {
                return false;
            }
            else
            {
                swallow = true;
                if (isDown)
                {
                    if (!_hotkeyDown) // ignore auto-repeat
                    {
                        _hotkeyDown = true;
                        changed = true;
                        if (_settings.Mode == ClickMode.Hold)
                            engine.Start(_settings.Cps, _settings.Button);
                        else if (engine.IsRunning)
                            engine.Stop();
                        else
                            engine.Start(_settings.Cps, _settings.Button);
                    }
                }
                else
                {
                    _hotkeyDown = false;
                    if (_settings.Mode == ClickMode.Hold) { engine.Stop(); changed = true; }
                }
            }
        }

        if (captured is not null) Captured?.Invoke(captured);
        if (changed) StateChanged?.Invoke();
        return swallow;
    }
}
