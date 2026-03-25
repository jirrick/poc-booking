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

        // Real API: { data: { conversation: {...}, ok: true } }
        var wrapper = JsonSerializer.Deserialize<ConversationDetailWrapper>(body, JsonOptions);
        return new BookingApiResponse<ConversationDetailResponse>
        {
            Data = wrapper?.Data?.Conversation,
            StatusCode = (int)response.StatusCode,
        };
    }

    public async Task<BookingApiResponse<PostMessageResponse>?> PostMessageAsync(string propertyId, string conversationId, string content, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations/{Uri.EscapeDataString(conversationId)}";
        var payload = new { message = new { content } };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var response = await _http.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new BookingApiResponse<PostMessageResponse> { Error = body, StatusCode = (int)response.StatusCode };

        // Real API: { data: { message_id, ok, guest_has_account } }
        var wrapper = JsonSerializer.Deserialize<PostMessageWrapper>(body, JsonOptions);
        return new BookingApiResponse<PostMessageResponse> { Data = wrapper?.Data, StatusCode = (int)response.StatusCode };
    }

    public async Task<BookingApiResponse<TagResponse>?> SetNoReplyNeededAsync(string propertyId, string conversationId, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations/{Uri.EscapeDataString(conversationId)}/tags/no_reply_needed";
        var response = await _http.PutAsync(path, new StringContent("", Encoding.UTF8, "application/json"), cancellationToken);
        return await DeserializeTagResponse(response, cancellationToken);
    }

    public async Task<BookingApiResponse<TagResponse>?> RemoveNoReplyNeededAsync(string propertyId, string conversationId, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations/{Uri.EscapeDataString(conversationId)}/tags/no_reply_needed";
        var response = await _http.DeleteAsync(path, cancellationToken);
        return await DeserializeTagResponse(response, cancellationToken);
    }

    public async Task<BookingApiResponse<TagResponse>?> SetMessageReadAsync(string propertyId, string conversationId, IEnumerable<string> messageIds, string participantId, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations/{Uri.EscapeDataString(conversationId)}/tags/message_read";
        var payload = new { message_ids = messageIds.ToArray(), participant_id = participantId };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var response = await _http.PutAsync(path, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        return await DeserializeTagResponse(response, cancellationToken);
    }

    public async Task<BookingApiResponse<TagResponse>?> RemoveMessageReadAsync(string propertyId, string conversationId, IEnumerable<string> messageIds, string participantId, CancellationToken cancellationToken = default)
    {
        var path = $"/messaging/properties/{Uri.EscapeDataString(propertyId)}/conversations/{Uri.EscapeDataString(conversationId)}/tags/message_read";
        var payload = new { message_ids = messageIds.ToArray(), participant_id = participantId };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Delete, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var response = await _http.SendAsync(request, cancellationToken);
        return await DeserializeTagResponse(response, cancellationToken);
    }

    private async Task<BookingApiResponse<TagResponse>?> DeserializeTagResponse(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return new BookingApiResponse<TagResponse> { Error = body, StatusCode = (int)response.StatusCode };
        var wrapper = JsonSerializer.Deserialize<TagWrapper>(body, JsonOptions);
        return new BookingApiResponse<TagResponse> { Data = wrapper?.Data, StatusCode = (int)response.StatusCode };
    }

    // ── Private wrappers ──────────────────────────────────────────────────────

    private sealed class ConversationListWrapper
    {
        public ConversationListResponse? Data { get; set; }
    }

    private sealed class ConversationDetailWrapper
    {
        public ConversationDetailData? Data { get; set; }
    }

    private sealed class ConversationDetailData
    {
        public ConversationDetailResponse? Conversation { get; set; }
    }

    private sealed class PostMessageWrapper
    {
        public PostMessageResponse? Data { get; set; }
    }

    private sealed class TagWrapper
    {
        public TagResponse? Data { get; set; }
    }
}
