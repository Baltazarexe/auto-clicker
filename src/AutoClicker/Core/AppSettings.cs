namespace AutoClicker.Core;

public sealed record AppSettings(int Cps, ClickButton Button, ClickMode Mode, Hotkey Hotkey)
{
    public const int MinCps = 1;
    public const int MaxCps = 50000;

    public static AppSettings Default { get; } = new(1000, ClickButton.Left, ClickMode.Toggle, Hotkey.Default);

    public static int ClampCps(long value) => (int)Math.Clamp(value, MinCps, MaxCps);
}
