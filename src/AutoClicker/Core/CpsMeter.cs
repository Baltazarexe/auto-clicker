namespace AutoClicker.Core;

/// <summary>Counts clicks over the last second in 100 buckets of 10 ms. Thread-safe.</summary>
public sealed class CpsMeter
{
    private const int Buckets = 100;

    private readonly long _bucketTicks;
    private readonly long[] _index = new long[Buckets];
    private readonly int[] _count = new int[Buckets];
    private readonly object _lock = new();

    public CpsMeter(long ticksPerSecond)
    {
        _bucketTicks = Math.Max(1, ticksPerSecond / Buckets);
        Array.Fill(_index, long.MinValue);
    }

    public void Add(long nowTicks, int count)
    {
        long b = nowTicks / _bucketTicks;
        int slot = (int)(b % Buckets);
        lock (_lock)
        {
            if (_index[slot] != b) { _index[slot] = b; _count[slot] = 0; }
            _count[slot] += count;
        }
    }

    public int Current(long nowTicks)
    {
        long b = nowTicks / _bucketTicks;
        int sum = 0;
        lock (_lock)
        {
            for (int i = 0; i < Buckets; i++)
                if (_index[i] > b - Buckets && _index[i] <= b) sum += _count[i];
        }
        return sum;
    }

    public void Clear()
    {
        lock (_lock) { Array.Fill(_index, long.MinValue); Array.Clear(_count); }
    }
}
