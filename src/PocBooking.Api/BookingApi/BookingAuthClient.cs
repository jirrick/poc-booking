using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PocBooking.Api.BookingApi;

public interface IBookingAuthClient
{
    Task<BookingAuthResult> ExchangeAsync(string clientId, string clientSecret, CancellationToken ct = default);
}

public sealed class BookingAuthResult
{
    public string? Jwt { get; init; }
    public string? Ruid { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess => Jwt != null && Error == null;
}

public sealed class BookingAuthClient : IBookingAuthClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public BookingAuthClient(HttpClient http, IOptions<BookingApiOptions> options)
    {
        _http = http;
        var baseUrl = (options.Value.AuthBaseUrl ?? options.Value.ApiBaseUrl)?.TrimEnd('/');
        if (!string.IsNullOrEmpty(baseUrl))
            _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<BookingAuthResult> ExchangeAsync(string clientId, string clientSecret, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { client_id = clientId, client_secret = clientSecret });
            var response = await _http.PostAsync(
                "/token-based-authentication/exchange",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
                ct);

            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new BookingAuthResult { Error = $"HTTP {(int)response.StatusCode}: {raw}" };

            var parsed = JsonSerializer.Deserialize<TokenExchangeResponse>(raw, JsonOpts);
            if (string.IsNullOrEmpty(parsed?.Jwt))
                return new BookingAuthResult { Error = "Response did not contain a JWT." };

            return new BookingAuthResult
            {
                Jwt = parsed.Jwt,
                Ruid = parsed.Ruid,
                ExpiresAt = ParseExpiry(parsed.Jwt)
            };
        }
        catch (Exception ex)
        {
            return new BookingAuthResult { Error = ex.Message };
        }
    }

    private static DateTimeOffset ParseExpiry(string jwt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(jwt))
            {
                var token = handler.ReadJwtToken(jwt);
                if (token.ValidTo != DateTime.MinValue)
                    return new DateTimeOffset(token.ValidTo, TimeSpan.Zero);
            }
        }
        catch { /* ignore */ }
        return DateTimeOffset.UtcNow.AddHours(1);
    }

    private sealed class TokenExchangeResponse
    {
        [JsonPropertyName("jwt")] public string? Jwt { get; init; }
        [JsonPropertyName("ruid")] public string? Ruid { get; init; }
    }
}
