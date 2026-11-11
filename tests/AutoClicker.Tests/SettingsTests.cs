using AutoClicker.Core;

namespace AutoClicker.Tests;

public class SettingsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ac-tests-" + Guid.NewGuid().ToString("N"));
    private string PathIn(string name) => System.IO.Path.Combine(_dir, name);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1000, 1000)]
    [InlineData(999999, 50000)]
    public void ClampCps_limits_range(long input, int expected) =>
        Assert.Equal(expected, AppSettings.ClampCps(input));

    [Fact]
    public void Load_missing_file_returns_default() =>
        Assert.Equal(AppSettings.Default, new SettingsStore(PathIn("missing.json")).Load());

    [Fact]
    public void Load_corrupt_file_returns_default()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathIn("s.json"), "{not json");
        Assert.Equal(AppSettings.Default, new SettingsStore(PathIn("s.json")).Load());
    }

    [Fact]
    public void Load_out_of_range_cps_is_clamped()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathIn("s.json"), """{"cps":999999,"button":"Left","mode":"Toggle","hotkey":"F6"}""");
        Assert.Equal(50000, new SettingsStore(PathIn("s.json")).Load().Cps);
    }

    [Fact]
    public void Load_invalid_field_falls_back_per_field()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathIn("s.json"), """{"cps":2000,"button":"Banana","mode":"Hold","hotkey":"???"}""");
        var s = new SettingsStore(PathIn("s.json")).Load();
        Assert.Equal(2000, s.Cps);
        Assert.Equal(ClickButton.Left, s.Button);
        Assert.Equal(ClickMode.Hold, s.Mode);
        Assert.Equal(Hotkey.Default, s.Hotkey);
    }

    [Fact]
    public void Save_then_Load_roundtrips()
    {
        Hotkey.TryParse("XButton2", out var hk);
        var settings = new AppSettings(2500, ClickButton.Right, ClickMode.Hold, hk);
        var store = new SettingsStore(PathIn("s.json"));
        store.Save(settings);
        Assert.Equal(settings, store.Load());
    }

    [Fact]
    public void Save_creates_missing_directory()
    {
        var store = new SettingsStore(PathIn(System.IO.Path.Combine("nested", "s.json")));
        store.Save(AppSettings.Default);
        Assert.True(File.Exists(PathIn(System.IO.Path.Combine("nested", "s.json"))));
    }

    [Fact]
    public void Load_duplicate_keys_does_not_throw()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathIn("s.json"), """{"cps":1,"cps":2,"button":"Left","button":"Right"}""");
        var s = new SettingsStore(PathIn("s.json")).Load();
        Assert.Equal(AppSettings.Default, s);
    }
}
