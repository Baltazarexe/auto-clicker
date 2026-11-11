using System.Diagnostics;
using AutoClicker.Native;

namespace AutoClicker.Core;

public interface IClickEngine
{
    void Start(int cps, ClickButton button);
    void Stop();
    void Update(int cps, ClickButton button);
    bool IsRunning { get; }
}

/// <summary>Click engine on a dedicated high-priority thread.</summary>
public sealed class ClickEngine(IClickSender sender) : IClickEngine, IDisposable
{
    private static readonly long OneMs = Stopwatch.Frequency / 1000;

    private readonly object _lock = new();
    private readonly CpsMeter _meter = new(Stopwatch.Frequency);
    private volatile int _generation;
    private volatile bool _running;
    private volatile bool _dirty;
    private int _cps;
    private ClickButton _button;

    public event Action<bool>? RunningChanged;

    public bool IsRunning => _running;

    public int RealCps => _running ? _meter.Current(Stopwatch.GetTimestamp()) : 0;

    public void Start(int cps, ClickButton button)
    {
        lock (_lock)
        {
            if (_running) { SetTarget(cps, button); return; }
            SetTarget(cps, button);
            _meter.Clear();
            _running = true;
            int generation = ++_generation;
            var thread = new Thread(() => Loop(generation))
            {
                IsBackground = true, Priority = ThreadPriority.Highest, Name = "ClickEngine",
            };
            thread.Start();
        }
        RunningChanged?.Invoke(true);
    }

    /// <summary>
    /// Signals the stop and returns immediately: never waits for the engine thread. This matters
    /// because Stop is called from inside the low-level hook, and the engine thread's SendInput
    /// may be waiting on that same hook - waiting here would deadlock.
    /// The in-flight batch (complete down+up pairs) finishes and the thread exits on its own.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!_running) return;
            _running = false;
            _generation++;
        }
        RunningChanged?.Invoke(false);
    }

    public void Update(int cps, ClickButton button)
    {
        lock (_lock) SetTarget(cps, button);
    }

    public void Dispose() => Stop();

    private void SetTarget(int cps, ClickButton button)
    {
        _cps = AppSettings.ClampCps(cps);
        _button = button;
        _dirty = true;
    }

    private void Loop(int generation)
    {
        // timeBegin/EndPeriod paired inside the thread itself: always balanced.
        NativeMethods.timeBeginPeriod(1);
        try { Run(generation); }
        finally { NativeMethods.timeEndPeriod(1); }
    }

    private void Run(int generation)
    {
        var scheduler = new ClickScheduler(Stopwatch.Frequency);
        var button = ClickButton.Left;

        while (_generation == generation)
        {
            long now = Stopwatch.GetTimestamp();
            if (_dirty)
            {
                lock (_lock)
                {
                    _dirty = false;
                    scheduler.Reset(now, _cps);
                    button = _button;
                }
            }

            int n = scheduler.Due(now);
            if (n > 0)
            {
                sender.Click(button, n);
                _meter.Add(now, n);
                continue;
            }

            // Intervals >= 1 ms: sleep (timeBeginPeriod(1) makes Sleep precise) and the
            // scheduler's "debt" makes up for any delay. Below 1 ms: yield the CPU without sleeping.
            if (scheduler.TicksUntilNext(now) >= OneMs) Thread.Sleep(1);
            else Thread.Yield();
        }
    }
}
