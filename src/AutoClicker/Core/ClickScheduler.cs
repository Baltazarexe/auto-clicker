namespace AutoClicker.Core;

/// <summary>
/// Schedules clicks by "debt": on each query it computes how many clicks
/// should have gone out since Reset and returns the difference. Not thread-safe;
/// owned by the engine thread.
/// </summary>
public sealed class ClickScheduler(long ticksPerSecond)
{
    public const int MaxBatch = 512;

    private long _start;
    private long _sent;
    private int _cps = 1;

    public void Reset(long nowTicks, int cps)
    {
        _start = nowTicks;
        _sent = 0;
        _cps = Math.Max(1, cps);
    }

    public int Due(long nowTicks)
    {
        long expected = Expected(nowTicks);
        long maxBehind = Math.Max(1, _cps / 20); // at most 50 ms of accumulated backlog
        if (expected - _sent > maxBehind) _sent = expected - maxBehind;
        long n = Math.Min(expected - _sent, MaxBatch);
        if (n <= 0) return 0;
        _sent += n;
        return (int)n;
    }

    /// <summary>Ticks until the next click is due (0 if already due).</summary>
    public long TicksUntilNext(long nowTicks)
    {
        Int128 numerator = (Int128)(_sent + 1) * ticksPerSecond;
        long offset = (long)((numerator + _cps - 1) / _cps);
        return Math.Max(0, _start + offset - nowTicks);
    }

    private long Expected(long nowTicks) =>
        nowTicks <= _start ? 0 : (long)((Int128)(nowTicks - _start) * _cps / ticksPerSecond);
}
