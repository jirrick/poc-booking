namespace PocBooking.Api.Auth;

/// <summary>
/// Validates the Bearer token on incoming CNS webhook requests.
/// When JWT config is set, validates signature, exp, iss, aud. Otherwise can allow through or require presence only.
/// </summary>
public interface ICnsSignatureValidator
{
    /// <summary>
    /// Validates the Authorization header (Bearer token). Returns null if valid, or an error message for 401.
    /// </summary>
    Task<string?> ValidateBearerAsync(string? authorizationHeader, CancellationToken cancellationToken = default);
}
