using System.Windows.Input;

namespace AutoClicker.Core;

public enum HotkeyKind { Keyboard, Mouse }

/// <summary>
/// Global hotkey. For the keyboard, <see cref="Code"/> is the virtual-key code;
/// for the mouse, 1 = XButton1 and 2 = XButton2.
/// </summary>
public sealed record Hotkey(HotkeyKind Kind, int Code)
{
    public static Hotkey Default { get; } = new(HotkeyKind.Keyboard, 0x75); // F6
    public static Hotkey Escape { get; } = new(HotkeyKind.Keyboard, 0x1B);

    public static bool TryParse(string? s, out Hotkey hotkey)
    {
        hotkey = Default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        if (s == "XButton1") { hotkey = new(HotkeyKind.Mouse, 1); return true; }
        if (s == "XButton2") { hotkey = new(HotkeyKind.Mouse, 2); return true; }

        if (!Enum.TryParse<Key>(s, ignoreCase: false, out var key) || key == Key.None) return false;
        if (!Enum.IsDefined(key)) return false;
        int vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0) return false;
        hotkey = new(HotkeyKind.Keyboard, vk);
        return true;
    }

    /// <summary>
    /// Side buttons and keys like F1-F12, numpad, Insert etc. can be hotkeys. Letters,
    /// digits, Space/Enter/Tab/Backspace, modifiers and Win cannot, because the hotkey
    /// is swallowed system-wide. Keys with no WPF name are rejected too (they cannot be saved).
    /// </summary>
    public bool IsAllowedAsHotkey
    {
        get
        {
            if (Kind == HotkeyKind.Mouse) return Code is 1 or 2;
            bool typing = Code is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A
                or 0x08 or 0x09 or 0x0D or 0x14 or 0x20 or 0x1B
                or >= 0x10 and <= 0x12 or >= 0xA0 and <= 0xA5 or 0x5B or 0x5C;
            return !typing && KeyInterop.KeyFromVirtualKey(Code) != Key.None;
        }
    }

    public override string ToString() => Kind switch
    {
        HotkeyKind.Mouse => Code == 1 ? "XButton1" : "XButton2",
        _ => KeyInterop.KeyFromVirtualKey(Code).ToString(),
    };
}
