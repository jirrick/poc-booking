using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PocBooking.BookingSimulator.Services;

namespace PocBooking.BookingSimulator.Endpoints;

public static class AuthEndpoints
{
    private const string BasePath = "/token-based-authentication";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // Simulates: POST https://connectivity-authentication.booking.com/token-based-authentication/exchange
        routes.MapPost($"{BasePath}/exchange", ExchangeToken)
            .WithName("ConnectivityAuthExchange");

        // JWKS endpoint so callers can validate the issued RS256 tokens
        routes.MapGet($"{BasePath}/.well-known/jwks.json", GetJwks)
            .WithName("ConnectivityAuthJwks");
    }

    // ── POST /token-based-authentication/exchange ──────────────────────────────

    private static IResult ExchangeToken(
        TokenExchangeRequest? body,
        IOptions<ConnectivityAuthOptions> optionsAccessor,
        SimulatorRsaKeyProvider rsaKeyProvider)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.ClientId) || string.IsNullOrWhiteSpace(body.ClientSecret))
        {
            return Results.Json(
                new { error = "invalid_request", error_description = "client_id and client_secret are required" },
                statusCode: 400,
                options: JsonOptions);
        }

        var opts = optionsAccessor.Value;

        // Validate credentials only when both are configured — otherwise the simulator is open.
        if (!string.IsNullOrEmpty(opts.ClientId) && !string.IsNullOrEmpty(opts.ClientSecret))
        {
            if (body.ClientId != opts.ClientId || body.ClientSecret != opts.ClientSecret)
            {
                return Results.Json(
                    new { error = "invalid_client", error_description = "Invalid client_id or client_secret" },
                    statusCode: 401,
                    options: JsonOptions);
            }
        }

        var now = DateTime.UtcNow;
        var signingKey = rsaKeyProvider.GetSigningKey();
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        // Build the JWT matching the real Booking.com connectivity-auth shape.
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "urn://connectivity-modern-auth/v1",
            Audience = string.Empty,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddHours(1),
            SigningCredentials = creds,
            // Extra claims that go beyond the standard registered names
            Claims = new Dictionary<string, object>
            {
                ["sub"] = "connectivity-auth-proxy",
                ["test"] = "false",
                ["machine_account_id"] = opts.MachineAccountId,
                ["provider_id"] = opts.ProviderId,
                ["client_id"] = body.ClientId,
                ["jti"] = Guid.NewGuid().ToString()
            }
        };

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.CreateEncodedJwt(descriptor);

        return Results.Json(new { jwt, ruid = Guid.NewGuid().ToString() }, options: JsonOptions);
    }

    // ── GET /token-based-authentication/.well-known/jwks.json ─────────────────

    private static IResult GetJwks(SimulatorRsaKeyProvider rsaKeyProvider)
    {
        var jwk = rsaKeyProvider.GetPublicJwk();
        return Results.Json(new { keys = new[] { jwk } });
    }
}

// ── Request model ─────────────────────────────────────────────────────────────

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class TokenExchangeRequest
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; init; }
}


