using AutoClicker.Core;

namespace AutoClicker.Tests;

internal sealed class FakeEngine : IClickEngine
{
    public int Starts, Stops;
    public List<(int Cps, ClickButton Button)> Updates { get; } = [];
    public bool IsRunning { get; private set; }

    public void Start(int cps, ClickButton button) { Starts++; IsRunning = true; }
    public void Stop() { if (IsRunning) Stops++; IsRunning = false; }
    public void Update(int cps, ClickButton button) => Updates.Add((cps, button));
}

public class ControllerTests
{
    private static readonly Hotkey F6 = Hotkey.Default;
    private static readonly Hotkey G = new(HotkeyKind.Keyboard, 0x47);
    private static readonly Hotkey F7 = new(HotkeyKind.Keyboard, 0x76);

    private static (ClickController c, FakeEngine e) Make(ClickMode mode = ClickMode.Toggle)
    {
        var e = new FakeEngine();
        return (new ClickController(e, AppSettings.Default with { Mode = mode }), e);
    }

    [Fact]
    public void Toggle_press_starts_second_press_stops()
    {
        var (c, e) = Make();
        Assert.True(c.OnHotkey(F6, true));
        c.OnHotkey(F6, false);
        Assert.True(e.IsRunning);
        c.OnHotkey(F6, true);
        c.OnHotkey(F6, false);
        Assert.False(e.IsRunning);
    }

    [Fact]
    public void Toggle_autorepeat_downs_do_not_flip()
    {
        var (c, e) = Make();
        c.OnHotkey(F6, true); c.OnHotkey(F6, true); c.OnHotkey(F6, true); c.OnHotkey(F6, false);
        Assert.True(e.IsRunning);
        Assert.Equal(1, e.Starts);
        c.OnHotkey(F6, true); c.OnHotkey(F6, false);
        Assert.False(e.IsRunning);
    }

    [Fact]
    public void Hold_runs_only_while_down()
    {
        var (c, e) = Make(ClickMode.Hold);
        c.OnHotkey(F6, true);
        Assert.True(e.IsRunning);
        c.OnHotkey(F6, true);
        Assert.Equal(1, e.Starts);
        c.OnHotkey(F6, false);
        Assert.False(e.IsRunning);
    }

    [Fact]
    public void Other_keys_are_ignored()
    {
        var (c, e) = Make();
        Assert.False(c.OnHotkey(G, true));
        Assert.False(c.OnHotkey(G, false));
        Assert.Equal(0, e.Starts);
    }

    [Fact]
    public void Capture_sets_hotkey_and_does_not_start()
    {
        var (c, e) = Make();
        Hotkey? captured = null;
        c.Captured += h => captured = h;
        c.BeginCapture();
        Assert.True(c.IsCapturing);
        Assert.True(c.OnHotkey(F7, true));
        c.OnHotkey(F7, false);
        Assert.Equal(F7, captured);
        Assert.Equal(F7, c.Settings.Hotkey);
        Assert.False(c.IsCapturing);
        Assert.Equal(0, e.Starts);
    }

    [Fact]
    public void Capture_with_current_hotkey_does_not_start()
    {
        var (c, e) = Make();
        c.BeginCapture();
        c.OnHotkey(F6, true);
        c.OnHotkey(F6, false);
        Assert.Equal(0, e.Starts);
        Assert.Equal(F6, c.Settings.Hotkey);
    }

    [Fact]
    public void Capture_escape_cancels()
    {
        var (c, _) = Make();
        bool fired = false;
        c.Captured += _ => fired = true;
        c.BeginCapture();
        c.OnHotkey(Hotkey.Escape, true);
        Assert.False(fired);
        Assert.False(c.IsCapturing);
        Assert.Equal(F6, c.Settings.Hotkey);
    }

    [Fact]
    public void Capture_stops_running_engine()
    {
        var (c, e) = Make();
        c.OnHotkey(F6, true); c.OnHotkey(F6, false);
        c.BeginCapture();
        Assert.False(e.IsRunning);
    }

    [Fact]
    public void Apply_mode_change_stops_engine()
    {
        var (c, e) = Make();
        c.ToggleFromButton();
        Assert.True(e.IsRunning);
        c.Apply(c.Settings with { Mode = ClickMode.Hold });
        Assert.False(e.IsRunning);
    }

    [Fact]
    public void Apply_hotkey_change_stops_engine()
    {
        var (c, e) = Make();
        c.ToggleFromButton();
        c.Apply(c.Settings with { Hotkey = G });
        Assert.False(e.IsRunning);
    }

    [Fact]
    public void Apply_cps_change_updates_live()
    {
        var (c, e) = Make();
        c.ToggleFromButton();
        c.Apply(c.Settings with { Cps = 3000 });
        Assert.True(e.IsRunning);
        Assert.Contains((3000, ClickButton.Left), e.Updates);
    }

    [Fact]
    public void ToggleFromButton_ignored_in_hold()
    {
        var (c, e) = Make(ClickMode.Hold);
        c.ToggleFromButton();
        Assert.False(e.IsRunning);
    }

    [Fact]
    public void StateChanged_fires_on_toggle()
    {
        var (c, _) = Make();
        int n = 0;
        c.StateChanged += () => n++;
        c.ToggleFromButton();
        c.ToggleFromButton();
        Assert.Equal(2, n);
    }

    [Fact]
    public void Capture_then_autorepeat_does_not_start()
    {
        var (c, e) = Make(ClickMode.Hold);
        var f7 = new Hotkey(HotkeyKind.Keyboard, 0x76);
        c.BeginCapture();
        c.OnHotkey(f7, true);
        c.OnHotkey(f7, true);
        c.OnHotkey(f7, true);
        c.OnHotkey(f7, false);
        Assert.Equal(0, e.Starts);
        c.OnHotkey(f7, true);
        Assert.True(e.IsRunning);
        c.OnHotkey(f7, false);
        Assert.False(e.IsRunning);
    }

    [Theory]
    [InlineData(0x41)] // A
    [InlineData(0x57)] // W
    [InlineData(0x31)] // 1
    [InlineData(0x20)] // Space
    [InlineData(0x0D)] // Enter
    [InlineData(0x10)] // Shift
    [InlineData(0xA2)] // LCtrl
    [InlineData(0x5B)] // LWin
    public void Capture_ignores_typing_keys(int vk)
    {
        var (c, _) = Make();
        bool fired = false;
        c.Captured += _ => fired = true;
        c.BeginCapture();
        c.OnHotkey(new Hotkey(HotkeyKind.Keyboard, vk), true);
        Assert.False(fired);
        Assert.True(c.IsCapturing);
        Assert.Equal(F6, c.Settings.Hotkey);
    }
}
