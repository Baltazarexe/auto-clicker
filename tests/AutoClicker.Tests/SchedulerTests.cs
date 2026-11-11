using AutoClicker.Core;

namespace AutoClicker.Tests;

public class SchedulerTests
{
    private const long T = 10_000_000; // fake ticks/s

    private static int RunFor(ClickScheduler s, long step)
    {
        int total = 0;
        for (long t = 0; t <= T; t += step) total += s.Due(t);
        return total;
    }

    [Fact]
    public void Due_1000cps_over_1s_totals_1000()
    {
        var s = new ClickScheduler(T);
        s.Reset(0, 1000);
        Assert.InRange(RunFor(s, T / 1000), 999, 1001);
    }

    [Fact]
    public void Due_5000cps_over_1s_totals_5000()
    {
        var s = new ClickScheduler(T);
        s.Reset(0, 5000);
        Assert.InRange(RunFor(s, T / 2000), 4999, 5001);
    }

    [Fact]
    public void Due_50000cps_over_1s_with_coarse_steps_totals_50000()
    {
        var s = new ClickScheduler(T);
        s.Reset(0, 50000);
        Assert.InRange(RunFor(s, T / 1000), 49990, 50001);
    }

    [Fact]
    public void Due_is_zero_before_first_interval()
    {
        var s = new ClickScheduler(T);
        s.Reset(0, 10);
        Assert.Equal(0, s.Due(T / 20));
    }

    [Fact]
    public void Due_after_200ms_stall_caps_burst()
    {
        var s = new ClickScheduler(T);
        s.Reset(0, 1000);
        Assert.True(s.Due(T / 5) <= 50);
    }

    [Fact]
    public void TicksUntilNext_is_interval_after_reset()
    {
        var s = new ClickScheduler(T);
        s.Reset(0, 1000);
        Assert.Equal(T / 1000, s.TicksUntilNext(0));
    }
}

public class CpsMeterTests
{
    private const long T = 10_000_000;

    [Fact]
    public void Meter_counts_last_second_only()
    {
        var m = new CpsMeter(T);
        m.Add(0, 500);
        m.Add(T / 2, 500);
        Assert.Equal(1000, m.Current(T / 2));
        Assert.Equal(500, m.Current(T + T / 4));
    }

    [Fact]
    public void Meter_is_zero_after_long_idle()
    {
        var m = new CpsMeter(T);
        m.Add(0, 500);
        Assert.Equal(0, m.Current(T * 5));
    }
}
