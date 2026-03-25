namespace PocBooking.Api.BookingApi;

/// <summary>
/// Configuration for outbound calls to the Booking API (simulator or real). Binds from "Booking".
/// Authentication is token-based only — use the home page to exchange credentials for a JWT.
/// </summary>
public sealed class BookingApiOptions
{
    public const string SectionName = "Booking";

    /// <summary>Base URL of the messaging API (e.g. http://localhost:5160 for simulator).</summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// Base URL of the connectivity-authentication endpoint. Defaults to <see cref="ApiBaseUrl"/> when not set.
    /// For the real Booking.com API this is https://connectivity-authentication.booking.com.
    /// </summary>
    public string? AuthBaseUrl { get; set; }

    /// <summary>client_id for token exchange (Booking:ClientId).</summary>
    public string? ClientId { get; set; }

    /// <summary>client_secret for token exchange (Booking:ClientSecret).</summary>
    public string? ClientSecret { get; set; }
}
