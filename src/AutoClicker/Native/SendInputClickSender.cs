using System.Runtime.InteropServices;
using AutoClicker.Core;
using static AutoClicker.Native.NativeMethods;

namespace AutoClicker.Native;

/// <summary>
/// Sends real clicks with SendInput. Each batch goes in a single call, always as
/// down+up pairs, so a button is never left "stuck". Used only by the engine thread.
/// </summary>
public sealed class SendInputClickSender : IClickSender
{
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();
    private readonly INPUT[] _buffer = new INPUT[ClickScheduler.MaxBatch * 2];

    public void Click(ClickButton button, int count)
    {
        var (down, up) = button switch
        {
            ClickButton.Right => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
            ClickButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
            _ => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP),
        };

        while (count > 0)
        {
            int n = Math.Min(count, ClickScheduler.MaxBatch);
            for (int i = 0; i < n; i++)
            {
                _buffer[2 * i] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = down } };
                _buffer[2 * i + 1] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = up } };
            }
            SendInput((uint)(2 * n), _buffer, InputSize);
            count -= n;
        }
    }
}
