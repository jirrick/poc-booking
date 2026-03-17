using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PocBooking.Api.Auth;

public sealed class CnsJwtSignatureValidator : ICnsSignatureValidator
{
    private readonly CnsJwtOptions _options;

    public CnsJwtSignatureValidator(IOptions<CnsJwtOptions> options)
    {
        _options = options.Value;
    }

    public Task<string?> ValidateBearerAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
            return Task.FromResult<string?>("Missing Authorization header");

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<string?>("Invalid Authorization scheme");

        var token = authorizationHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return Task.FromResult<string?>("Missing Bearer token");

        // When no JWT config is set, we only require Bearer presence (handled by caller if RequireSignatureValidation is false).
        var signingKey = _options.JwtSigningKey;
        if (string.IsNullOrEmpty(signingKey))
        {
            if (_options.RequireSignatureValidation)
                return Task.FromResult<string?>("JWT validation is required but Booking:Cns:JwtSigningKey is not set");
            return Task.FromResult<string?>(null); // Allow through
        }

        var issuer = _options.JwtIssuer;
        var audience = _options.JwtAudience;
        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            return Task.FromResult<string?>("JWT validation requires Booking:Cns:JwtIssuer and Booking:Cns:JwtAudience");

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validationParams, out _);
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            return Task.FromResult<string?>($"Invalid JWT: {ex.Message}");
        }
    }
}
