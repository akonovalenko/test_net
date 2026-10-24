namespace MarketRateInfo.Server.Akka;

/// <summary>
/// Adaptive delay used by the Bitcoin stream. The implementation follows
/// the throttling strategy used by AkkaNetDemo: 0 -> 10 -> 20 -> 40 -> 60s.
/// </summary>
public sealed class AdaptiveThrottle
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(40),
        TimeSpan.FromSeconds(60)
    ];

    private int _level;

    public TimeSpan CurrentDelay => Delays[Volatile.Read(ref _level)];

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        var delay = CurrentDelay;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);
    }

    public void OnFailure()
    {
        var current = Volatile.Read(ref _level);
        if (current < Delays.Length - 1)
            Interlocked.CompareExchange(ref _level, current + 1, current);
    }

    public void OnSuccess() => Interlocked.Exchange(ref _level, 0);
}
