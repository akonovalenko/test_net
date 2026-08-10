namespace AkkaNetDemo;

public class RuntimeMetricPoint
{
    public DateTime Time { get; set; }
    public long GcHeapBytes { get; set; }
    public int ThreadPoolThreads { get; set; }
    public long WorkingSetBytes { get; set; }
    public double ThrottleDelayMs { get; set; }
}

public sealed class RuntimeMetricsStore
{
    private readonly List<RuntimeMetricPoint> _points = new();
    private readonly object _lock = new();

    public void Add(RuntimeMetricPoint point)
    {
        lock (_lock)
        {
            _points.Add(point);

            // keep last 1000 points
            if (_points.Count > 1000)
            {
                _points.RemoveAt(0);
            }
        }
    }

    public IReadOnlyList<RuntimeMetricPoint> GetAll()
    {
        lock (_lock)
        {
            return _points.ToList();
        }
    }
}