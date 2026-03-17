namespace PocBooking.Api.Auth;

/// <summary>
/// Configuration for CNS webhook JWT validation. Binds from "Booking:Cns".
/// </summary>
public sealed class CnsJwtOptions
{
    public const string SectionName = "Booking:Cns";

    /// <summary>Shared secret for symmetric JWT signing (simulator). Leave empty when using JWKS in production.</summary>
    public string? JwtSigningKey { get; set; }

    /// <summary>Expected JWT issuer.</summary>
    public string? JwtIssuer { get; set; }

    /// <summary>Expected JWT audience.</summary>
    public string? JwtAudience { get; set; }

    /// <summary>When true (default when key is set), reject requests with invalid/missing JWT.</summary>
    public bool RequireSignatureValidation { get; set; } = true;
}
