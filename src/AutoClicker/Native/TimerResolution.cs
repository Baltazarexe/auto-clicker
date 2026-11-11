using System.Runtime.InteropServices;

namespace AutoClicker.Native;

/// <summary>
/// On Windows 11, timeBeginPeriod(1) is ignored while the process windows are
/// minimized or covered (e.g. fullscreen Minecraft in front). Without this, Sleep(1)
/// becomes ~15.6 ms and clicks come out in bursts. This call opts out of that behavior.
/// </summary>
public static class TimerResolution
{
    private const int ProcessPowerThrottling = 4;
    private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
    private const uint PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION = 0x4;

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_POWER_THROTTLING_STATE
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessInformation(IntPtr hProcess, int infoClass,
        ref PROCESS_POWER_THROTTLING_STATE info, int size);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    /// <returns>true if Windows accepted it (false on older versions without support).</returns>
    public static bool KeepHighResolutionWhenHidden()
    {
        // ControlMask = IGNORE_TIMER_RESOLUTION and StateMask = 0 => "always honor the requested resolution".
        var state = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask = PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION,
            StateMask = 0,
        };
        return SetProcessInformation(GetCurrentProcess(), ProcessPowerThrottling, ref state,
            Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>());
    }
}
