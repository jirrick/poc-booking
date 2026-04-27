using System.Collections.Concurrent;

namespace PocBooking.BookingSimulator.Services;

public sealed class ApiRequestLogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Method { get; init; } = "";
    public string Path { get; init; } = "";
    public string? QueryString { get; init; }
    public int StatusCode { get; set; }
    public long ElapsedMs { get; set; }
    public string? RequestBody { get; init; }
    public Dictionary<string, string> RequestHeaders { get; init; } = new();
}

/// <summary>
/// Thread-safe ring buffer of the last N incoming API requests.
/// </summary>
public sealed class ApiRequestLogStore
{
    private const int MaxEntries = 200;
    private readonly ConcurrentQueue<ApiRequestLogEntry> _entries = new();

    public void Add(ApiRequestLogEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);
    }

    /// Returns entries newest-first.
    public IReadOnlyList<ApiRequestLogEntry> GetAll() =>
        _entries.Reverse().ToList();

    public void Clear() => _entries.Clear();
}

