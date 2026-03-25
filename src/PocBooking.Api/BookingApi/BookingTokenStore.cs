namespace PocBooking.Api.BookingApi;

/// <summary>
/// In-memory store for the connectivity JWT obtained from the Booking.com token exchange.
/// Registered as a singleton so the same token is shared across the request pipeline.
/// </summary>
public interface IBookingTokenStore
{
    string? Jwt { get; }
    DateTimeOffset? ExpiresAt { get; }

    /// <summary>True when a JWT is present and has more than 30 s left before it expires.</summary>
    bool IsValid { get; }

    void Set(string jwt, DateTimeOffset expiresAt);
    void Clear();
}

public sealed class BookingTokenStore : IBookingTokenStore
{
    private volatile TokenState? _state;

    public string? Jwt => _state?.Jwt;
    public DateTimeOffset? ExpiresAt => _state?.ExpiresAt;
    public bool IsValid => _state is { } s && s.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30);

    public void Set(string jwt, DateTimeOffset expiresAt) => _state = new TokenState(jwt, expiresAt);
    public void Clear() => _state = null;

    private sealed record TokenState(string Jwt, DateTimeOffset ExpiresAt);
}

