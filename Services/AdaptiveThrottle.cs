public sealed class AdaptiveThrottle
{
    // Delay levels: 0 -> 10 -> 20 -> 40 -> 60 seconds.
    private static readonly TimeSpan[] Delays =
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(40),
        TimeSpan.FromSeconds(60)
    };

    private int _level;

    public event Action? OnThrottleDecreasedToMinimum;

    public AdaptiveThrottle() => _level = 0;

    // Wait using the current adaptive delay before the next request.
    public async Task WaitAsync()
    {
        var delay = Delays[Volatile.Read(ref _level)];
        Console.WriteLine($"[THROTTLE] Waiting {delay.TotalSeconds:0}s");
        await Task.Delay(delay);
    }

    // Increase delay after a failed request: 0 -> 10 -> 20 -> 40 -> 60.
    public void OnFailure()
    {
        var oldLevel = Volatile.Read(ref _level);
        if (oldLevel >= Delays.Length - 1)
            return;

        var newLevel = Interlocked.Increment(ref _level);
        Console.WriteLine($"[THROTTLE] Increased to {Delays[newLevel].TotalSeconds:0}s");
    }

    // Successful request completely resets the throttle: 10/20/40/60 -> 0.
    public void OnSuccess()
    {
        var oldLevel = Interlocked.Exchange(ref _level, 0);
        if (oldLevel > 0)
        {
            Console.WriteLine($"[THROTTLE] Reset to {Delays[0].TotalSeconds:0}s");
            OnThrottleDecreasedToMinimum?.Invoke();
        }
    }

    // Current delay used by the next request.
    public TimeSpan CurrentDelay => Delays[Volatile.Read(ref _level)];
}