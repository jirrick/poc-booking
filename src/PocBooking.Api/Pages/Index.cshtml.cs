using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PocBooking.Api.BookingApi;

namespace PocBooking.Api.Pages;

public class IndexModel : PageModel
{
    private readonly IBookingTokenStore _tokenStore;
    private readonly IBookingAuthClient _authClient;
    private readonly IOptions<BookingApiOptions> _options;

    public IndexModel(IBookingTokenStore tokenStore, IBookingAuthClient authClient, IOptions<BookingApiOptions> options)
    {
        _tokenStore = tokenStore;
        _authClient = authClient;
        _options = options;
    }

    // ── Display properties ────────────────────────────────────────────────────

    public string DefaultPropertyId => _options.Value.DefaultPropertyId;

    public bool TokenIsValid => _tokenStore.IsValid;
    public DateTimeOffset? TokenExpiresAt => _tokenStore.ExpiresAt;
    public string? TokenJwt => _tokenStore.Jwt;

    /// <summary>Human-readable time-until-expiry (e.g. "54 min" or "Expired").</summary>
    public string TokenExpiryLabel
    {
        get
        {
            if (_tokenStore.ExpiresAt is not { } exp) return "—";
            var remaining = exp - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) return "Expired";
            if (remaining.TotalHours >= 1) return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            return $"{(int)remaining.TotalMinutes} min";
        }
    }

    /// <summary>Decoded JWT claims for display, null when no token.</summary>
    public Dictionary<string, string>? TokenClaims { get; private set; }

    [TempData] public string? TokenError { get; set; }
    [TempData] public string? TokenSuccess { get; set; }

    // ── Form binding ──────────────────────────────────────────────────────────

    [BindProperty] public string? ClientId { get; set; }
    [BindProperty] public string? ClientSecret { get; set; }

    // ── Handlers ──────────────────────────────────────────────────────────────

    public void OnGet()
    {
        // Pre-fill form from config so the user can click straight through in dev
        ClientId = _options.Value.ClientId;
        ClientSecret = _options.Value.ClientSecret;
        DecodeClaims();
    }

    public async Task<IActionResult> OnPostExchangeTokenAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
        {
            TokenError = "client_id and client_secret are required.";
            return RedirectToPage();
        }

        var result = await _authClient.ExchangeAsync(ClientId, ClientSecret, ct);
        if (!result.IsSuccess)
        {
            TokenError = result.Error ?? "Token exchange failed.";
        }
        else
        {
            _tokenStore.Set(result.Jwt!, result.ExpiresAt ?? DateTimeOffset.UtcNow.AddHours(1));
            TokenSuccess = $"Token acquired — valid until {_tokenStore.ExpiresAt:HH:mm:ss} UTC";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostClearToken()
    {
        _tokenStore.Clear();
        TokenSuccess = "Token cleared.";
        return RedirectToPage();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DecodeClaims()
    {
        var jwt = _tokenStore.Jwt;
        if (string.IsNullOrEmpty(jwt)) return;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(jwt)) return;
            var token = handler.ReadJwtToken(jwt);
            TokenClaims = token.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(c => c.Value)));
        }
        catch { /* ignore malformed tokens */ }
    }
}
