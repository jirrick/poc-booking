using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PocBooking.Api.BookingApi;

public sealed class BookingApiClient : IBookingApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public BookingApiClient(HttpClient http, IOptions<BookingApiOptions> options)
    {
        _http = http;
        var opts = options.Value;
        var baseUrl = opts.ApiBaseUrl?.TrimEnd('/');
        if (!string.IsNullOrEmpty(baseUrl))
            _http.BaseAddress = new Uri(baseUrl);
        var apiKey = opts.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<BookingApiResponse<ConversationListResponse>?> GetConversationsAsync(string propertyId, string? pageId = null, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations";
        if (!string.IsNullOrEmpty(pageId))
            path += "?page_id=" + Uri.EscapeDataString(pageId);
        var response = await _http.GetAsync(path, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new BookingApiResponse<ConversationListResponse> { Error = body, StatusCode = (int)response.StatusCode };

        var wrapper = JsonSerializer.Deserialize<ConversationListWrapper>(body, JsonOptions);
        return new BookingApiResponse<ConversationListResponse>
        {
            Data = wrapper?.Data,
            StatusCode = (int)response.StatusCode,
        };
    }

    public async Task<BookingApiResponse<ConversationDetailResponse>?> GetConversationAsync(string propertyId, string conversationId, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations/{Uri.EscapeDataString(conversationId)}";
        var response = await _http.GetAsync(path, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new BookingApiResponse<ConversationDetailResponse> { Error = body, StatusCode = (int)response.StatusCode };

        var wrapper = JsonSerializer.Deserialize<ConversationDetailWrapper>(body, JsonOptions);
        return new BookingApiResponse<ConversationDetailResponse>
        {
            Data = wrapper?.Data,
            StatusCode = (int)response.StatusCode,
        };
    }

    public async Task<BookingApiResponse<PostMessageResponse>?> PostMessageAsync(string propertyId, string conversationId, string content, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations/{Uri.EscapeDataString(conversationId)}";
        var payload = new { message = new { content = content } };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(path, requestContent, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new BookingApiResponse<PostMessageResponse> { Error = body, StatusCode = (int)response.StatusCode };

        var result = JsonSerializer.Deserialize<PostMessageResponse>(body, JsonOptions);
        return new BookingApiResponse<PostMessageResponse> { Data = result, StatusCode = (int)response.StatusCode };
    }

    private sealed class ConversationListWrapper
    {
        public ConversationListResponse? Data { get; set; }
    }

    private sealed class ConversationDetailWrapper
    {
        public ConversationDetailResponse? Data { get; set; }
    }
}
