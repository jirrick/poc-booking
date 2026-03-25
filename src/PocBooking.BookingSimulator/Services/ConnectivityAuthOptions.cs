namespace PocBooking.BookingSimulator.Services;

/// <summary>
/// Configuration for the simulated Booking.com token-based authentication endpoint.
/// Maps to the "BookingSimulator:Auth" section.
/// </summary>
public sealed class ConnectivityAuthOptions
{
    public const string SectionName = "BookingSimulator:Auth";

    /// <summary>Expected client_id. Leave empty to accept any credentials (open simulator).</summary>
    public string? ClientId { get; init; }

    /// <summary>Expected client_secret. Leave empty to accept any credentials (open simulator).</summary>
    public string? ClientSecret { get; init; }

    /// <summary>Value written into the machine_account_id JWT claim.</summary>
    public string MachineAccountId { get; init; } = "012345";

    /// <summary>Value written into the provider_id JWT claim.</summary>
    public string ProviderId { get; init; } = "1234";
}


