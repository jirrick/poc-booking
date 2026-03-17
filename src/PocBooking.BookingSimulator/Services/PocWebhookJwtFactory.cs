using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PocBooking.BookingSimulator.Services;

/// <summary>
/// Builds a short-lived JWT for the POC webhook when BookingSimulator JWT config is set (same secret/issuer/audience as POC).
/// </summary>
public interface IPocWebhookJwtFactory
{
    /// <summary>Returns a Bearer token (JWT if JWT config is set, otherwise null so caller uses PocBearerToken).</summary>
    string? CreateBearerToken();
}

public sealed class PocWebhookJwtOptions
{
    public const string SectionName = "BookingSimulator";

    public string? JwtSigningKey { get; set; }
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
}

public sealed class PocWebhookJwtFactory : IPocWebhookJwtFactory
{
    private readonly PocWebhookJwtOptions _options;

    public PocWebhookJwtFactory(IOptions<PocWebhookJwtOptions> options)
    {
        _options = options.Value;
    }

    public string? CreateBearerToken()
    {
        var key = _options.JwtSigningKey;
        var issuer = _options.JwtIssuer;
        var audience = _options.JwtAudience;
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            return null;

        var symKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(symKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
