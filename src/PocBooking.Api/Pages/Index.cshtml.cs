using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PocBooking.Api.BookingApi;
using PocBooking.Api.Llm;

namespace PocBooking.Api.Pages;

public class IndexModel : PageModel
{
    private readonly IBookingTokenStore _tokenStore;
    private readonly IBookingAuthClient _authClient;
    private readonly IOptions<BookingApiOptions> _options;
    private readonly ILlmEmailParser _llmParser;
    private readonly LlmSettingsStore _llmSettings;

    public IndexModel(
        IBookingTokenStore tokenStore,
        IBookingAuthClient authClient,
        IOptions<BookingApiOptions> options,
        ILlmEmailParser llmParser,
        LlmSettingsStore llmSettings)
    {
        _tokenStore = tokenStore;
        _authClient = authClient;
        _options = options;
        _llmParser = llmParser;
        _llmSettings = llmSettings;
    }

    // ── Display properties ────────────────────────────────────────────────────

    public string DefaultPropertyId => _options.Value.DefaultPropertyId;

    public bool TokenIsValid => _tokenStore.IsValid;
    public DateTimeOffset? TokenExpiresAt => _tokenStore.ExpiresAt;
    public string? TokenJwt => _tokenStore.Jwt;

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

    public Dictionary<string, string>? TokenClaims { get; private set; }

    [TempData] public string? TokenError { get; set; }
    [TempData] public string? TokenSuccess { get; set; }
    [TempData] public string? LlmSuccess { get; set; }
    [TempData] public string? LlmError { get; set; }

    // ── Booking form binding ──────────────────────────────────────────────────

    [BindProperty] public string? ClientId { get; set; }
    [BindProperty] public string? ClientSecret { get; set; }

    // ── LLM display / binding ─────────────────────────────────────────────────

    public IReadOnlyList<string> LlmModels { get; private set; } = [];
    public string? LlmCurrentModel => _llmSettings.SelectedModel;
    public string LlmSystemPrompt => _llmSettings.SystemPrompt;

    [BindProperty] public string? LlmModel { get; set; }
    [BindProperty] public string? LlmSystemPromptInput { get; set; }

    // ── Handlers ──────────────────────────────────────────────────────────────

    public async Task OnGetAsync(CancellationToken ct = default)
    {
        ClientId = _options.Value.ClientId;
        ClientSecret = _options.Value.ClientSecret;
        DecodeClaims();
        LlmModels = await _llmParser.GetModelsAsync(ct);
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

    public IActionResult OnPostSaveLlmSettings()
    {
        if (string.IsNullOrWhiteSpace(LlmModel))
        {
            LlmError = "Please select a model.";
            return RedirectToPage();
        }

        _llmSettings.SelectedModel = LlmModel.Trim();
        if (!string.IsNullOrWhiteSpace(LlmSystemPromptInput))
            _llmSettings.SystemPrompt = LlmSystemPromptInput;

        LlmSuccess = $"LLM settings saved — model: {_llmSettings.SelectedModel}";
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
