using System.Runtime.InteropServices;
using AutoClicker.Core;
using static AutoClicker.Native.NativeMethods;

namespace AutoClicker.Native;

/// <summary>
/// Global keyboard and mouse (side buttons) hooks on a dedicated thread with a message loop.
/// Injected events (including our own clicks) are ignored.
/// </summary>
public sealed class GlobalInputHook(Func<Hotkey, bool, bool> handler) : IDisposable
{
    // Delegates kept in fields so the GC does not collect them while Windows calls them.
    private LowLevelProc? _keyboardProc;
    private LowLevelProc? _mouseProc;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private Thread? _thread;
    private uint _threadId;
    private readonly ManualResetEventSlim _ready = new();

    public void Start()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "GlobalInputHook" };
        _thread.Start();
        _ready.Wait();
    }

    private void Run()
    {
        _threadId = GetCurrentThreadId();
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
        IntPtr module = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, module, 0);
        _ready.Set();

        while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0) { }

        if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
        _keyboardHook = _mouseHook = IntPtr.Zero;
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int msg = (int)wParam;
            bool down = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
            bool up = msg is WM_KEYUP or WM_SYSKEYUP;
            if ((data.flags & LLKHF_INJECTED) == 0 && (down || up))
            {
                if (SafeHandle(new Hotkey(HotkeyKind.Keyboard, (int)data.vkCode), down)) return 1;
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg is WM_XBUTTONDOWN or WM_XBUTTONUP)
            {
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int button = (int)(data.mouseData >> 16);
                if ((data.flags & LLMHF_INJECTED) == 0 && button is 1 or 2)
                {
                    if (SafeHandle(new Hotkey(HotkeyKind.Mouse, button), msg == WM_XBUTTONDOWN)) return 1;
                }
            }
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool SafeHandle(Hotkey key, bool down)
    {
        // An exception inside a low-level hook would crash the process.
        try { return handler(key, down); }
        catch { return false; }
    }

    public void Dispose()
    {
        if (_thread is null) return;
        PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(1000);
        _thread = null;
        _ready.Dispose();
    }
}
