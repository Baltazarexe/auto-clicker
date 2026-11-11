using AutoClicker.Core;
using AutoClicker.UI;

namespace AutoClicker.Tests;

public class ViewModelTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "ac-vm-" + Guid.NewGuid().ToString("N"), "settings.json");
    private readonly FakeEngine _engine = new();
    private readonly SettingsStore _store;
    private readonly ClickController _controller;
    private readonly MainViewModel _vm;

    public ViewModelTests()
    {
        _store = new SettingsStore(_path);
        _controller = new ClickController(_engine, AppSettings.Default);
        _vm = new MainViewModel(_controller, _store, () => 1004, a => a());
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_path)!;
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Fact]
    public void CpsText_invalid_reverts()
    {
        _vm.CpsText = "abc";
        _vm.CommitCps();
        Assert.Equal("1000", _vm.CpsText);
        Assert.Equal(1000, _controller.Settings.Cps);
    }

    [Fact]
    public void CpsText_huge_clamps()
    {
        _vm.CpsText = "999999";
        _vm.CommitCps();
        Assert.Equal("50000", _vm.CpsText);
        Assert.Equal(50000, _controller.Settings.Cps);
    }

    [Fact]
    public void CpsText_zero_clamps_to_one()
    {
        _vm.CpsText = "0";
        _vm.CommitCps();
        Assert.Equal("1", _vm.CpsText);
    }

    [Fact]
    public void PrimaryLabel_reflects_state()
    {
        Assert.Equal("Start", _vm.PrimaryLabel);
        _vm.PrimaryCommand.Execute(null);
        Assert.True(_vm.IsRunning);
        Assert.Equal("Stop", _vm.PrimaryLabel);
        _vm.Mode = ClickMode.Hold;
        Assert.Equal("Hold F6", _vm.PrimaryLabel);
        Assert.False(_vm.PrimaryEnabled);
    }

    [Fact]
    public void RealCpsLabel_uses_ptBR_format()
    {
        _vm.PrimaryCommand.Execute(null);
        _vm.Refresh();
        Assert.Equal("1,004 real CPS", _vm.RealCpsLabel);
    }

    [Fact]
    public void Changes_are_saved()
    {
        _vm.Button = ClickButton.Right;
        Assert.Equal(ClickButton.Right, _store.Load().Button);
    }

    [Fact]
    public void Captured_hotkey_is_saved_and_shown()
    {
        _vm.CaptureHotkeyCommand.Execute(null);
        Assert.Equal("Press a key…", _vm.HotkeyLabel);
        _controller.OnHotkey(new Hotkey(HotkeyKind.Mouse, 1), true);
        Assert.Equal("Mouse 4", _vm.HotkeyLabel);
        Assert.Equal("XButton1", _store.Load().Hotkey.ToString());
    }

    [Fact]
    public void Typed_cps_applies_without_commit()
    {
        _vm.CpsText = "5000";
        Assert.Equal(5000, _controller.Settings.Cps);
        Assert.Equal("5000", _vm.CpsText);
    }
}
