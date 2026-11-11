using AutoClicker.Core;

namespace AutoClicker.Tests;

internal sealed class FakeSender : IClickSender
{
    private int _total;
    public int Total => Volatile.Read(ref _total);
    public volatile bool SawRight;

    public void Click(ClickButton button, int count)
    {
        if (button == ClickButton.Right) SawRight = true;
        Interlocked.Add(ref _total, count);
    }
}

public class EngineTests
{
    [Fact]
    public void Engine_sends_and_stops()
    {
        var sender = new FakeSender();
        using var engine = new ClickEngine(sender);
        engine.Start(2000, ClickButton.Left);
        Assert.True(engine.IsRunning);
        Thread.Sleep(500);
        engine.Stop();
        Assert.False(engine.IsRunning);
        Thread.Sleep(30); // an in-flight batch may finish after Stop
        int total = sender.Total;
        Assert.InRange(total, 800, 1200);
        Thread.Sleep(100);
        Assert.Equal(total, sender.Total);
    }

    [Fact]
    public void Engine_update_changes_button_live()
    {
        var sender = new FakeSender();
        using var engine = new ClickEngine(sender);
        engine.Start(1000, ClickButton.Left);
        engine.Update(1000, ClickButton.Right);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!sender.SawRight && sw.ElapsedMilliseconds < 200) Thread.Sleep(1);
        engine.Stop();
        Assert.True(sender.SawRight);
        Assert.True(sw.ElapsedMilliseconds <= 50, $"took {sw.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void Engine_raises_RunningChanged()
    {
        var events = new List<bool>();
        using var engine = new ClickEngine(new FakeSender());
        engine.RunningChanged += events.Add;
        engine.Start(100, ClickButton.Left);
        engine.Stop();
        Assert.Equal([true, false], events);
    }

    [Fact]
    public void Engine_reports_real_cps_near_target()
    {
        using var engine = new ClickEngine(new FakeSender());
        engine.Start(1000, ClickButton.Left);
        Thread.Sleep(1100);
        int real = engine.RealCps;
        engine.Stop();
        Assert.InRange(real, 950, 1050);
    }

    [Fact]
    public void Stop_does_not_block_while_sender_is_busy()
    {
        var sender = new BlockingSender();
        using var engine = new ClickEngine(sender);
        engine.Start(1000, ClickButton.Left);
        Assert.True(sender.Entered.Wait(1000));
        var stop = Task.Run(engine.Stop);
        bool returned = stop.Wait(100);
        sender.Release.Set();
        Assert.True(returned, "Stop() blocked while SendInput was in progress");
    }
}

internal sealed class BlockingSender : IClickSender
{
    public readonly ManualResetEventSlim Entered = new();
    public readonly ManualResetEventSlim Release = new();
    public void Click(ClickButton button, int count) { Entered.Set(); Release.Wait(); }
}
