namespace PocBooking.Api.BookingApi;

/// <summary>
/// Configuration for outbound calls to Booking API (simulator or real). Binds from "Booking".
/// </summary>
public sealed class BookingApiOptions
{
    public const string SectionName = "Booking";

    /// <summary>Base URL of the Booking API (e.g. http://localhost:5160 for simulator).</summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>Optional API key or token for Authorization: Bearer (simulator uses BookingSimulator:ApiKey).</summary>
    public string? ApiKey { get; set; }
}
