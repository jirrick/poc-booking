using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PocBooking.BookingSimulator.Services;

/// <summary>
/// Obtains access tokens via OAuth client credentials flow and caches them.
/// Falls back to opaque bearer token if OAuth is disabled or misconfigured.
/// </summary>
public interface IOAuthTokenProvider
{
    /// <summary>Returns an access token for webhook authentication, or null if not available.</summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class OAuthWebhookOptions
{
    public const string SectionName = "BookingSimulator:OAuthWebhook";

    /// <summary>Enable OAuth client credentials flow for webhook authentication.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Token endpoint URL (e.g., https://auth.example.com/oauth/token).</summary>
    public string? TokenUrl { get; set; }

    /// <summary>OAuth client ID.</summary>
    public string? ClientId { get; set; }

    /// <summary>OAuth client secret.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Target webhook URL for CNS messages (e.g., https://api.example.com/webhooks/cns).</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>Optional scope for the access token.</summary>
    public string? Scope { get; set; }

    /// <summary>Fallback bearer token if OAuth is disabled.</summary>
    public string? FallbackBearerToken { get; set; }
}

internal sealed class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public sealed class OAuthTokenProvider : IOAuthTokenProvider
{
    private readonly OAuthWebhookOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthTokenProvider> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiryUtc;

    public OAuthTokenProvider(
        IOptions<OAuthWebhookOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthTokenProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tokenExpiryUtc = DateTime.UtcNow;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // If OAuth is disabled, return fallback token
        if (!_options.Enabled)
        {
            _logger.LogDebug("OAuth disabled; using fallback bearer token.");
            return _options.FallbackBearerToken;
        }

        // Validate required OAuth config
        if (string.IsNullOrEmpty(_options.TokenUrl) || string.IsNullOrEmpty(_options.ClientId) || string.IsNullOrEmpty(_options.ClientSecret))
        {
            _logger.LogDebug("OAuth config incomplete; falling back to bearer token.");
            return _options.FallbackBearerToken;
        }

        // Return cached token if still valid (with 5-min buffer before expiry)
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow.AddMinutes(5) < _tokenExpiryUtc)
        {
            _logger.LogDebug("Using cached access token.");
            return _cachedToken;
        }

        // Fetch new token
        try
        {
            _logger.LogDebug("Fetching new access token from {TokenUrl}", _options.TokenUrl);
            using var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _options.ClientId },
                    { "client_secret", _options.ClientSecret },
                })
            };

            if (!string.IsNullOrEmpty(_options.Scope))
            {
                var content = request.Content as FormUrlEncodedContent;
                // Re-create content with scope included
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _options.ClientId },
                    { "client_secret", _options.ClientSecret },
                    { "scope", _options.Scope },
                });
            }

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OAuth token request failed with status {Code}: {Content}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync(cancellationToken));
                return _options.FallbackBearerToken;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(jsonContent);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogWarning("OAuth token response missing access_token");
                return _options.FallbackBearerToken;
            }

            // Cache token with expiry
            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiryUtc = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            _logger.LogDebug("Cached access token, expires in {ExpiresIn} seconds", tokenResponse.ExpiresIn);

            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch OAuth token; falling back to bearer token");
            return _options.FallbackBearerToken;
        }
    }
}



