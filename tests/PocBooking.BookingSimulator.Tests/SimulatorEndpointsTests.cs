using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PocBooking.BookingSimulator.Tests;

public sealed class SimulatorEndpointsTests : IClassFixture<SimulatorWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly string PropertyId = "1383087";
    private static readonly string ConversationId = "f3a9c29d-480d-5f5b-a6c0-65451e335353";

    public SimulatorEndpointsTests(SimulatorWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_api_returns_ok_and_service_info()
    {
        var response = await _client.GetAsync("/api");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PocBooking.BookingSimulator", json.GetProperty("service").GetString());
    }

    [Fact]
    public async Task GET_api_simulate_sample_returns_ok_and_sample_payload()
    {
        var response = await _client.GetAsync("/api/simulate/sample");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MESSAGING_API_NEW_MESSAGE", json.GetProperty("metadata").GetProperty("type").GetString());
        Assert.NotNull(json.GetProperty("payload").GetProperty("message_id").GetString());
        Assert.NotNull(json.GetProperty("payload").GetProperty("content").GetString());
    }

    [Fact]
    public async Task POST_api_simulate_deliver_returns_ok()
    {
        var response = await _client.PostAsync("/api/simulate/deliver", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task GET_messaging_properties_propertyId_conversations_returns_ok_and_conversations()
    {
        var response = await _client.GetAsync($"/messaging/properties/{PropertyId}/conversations");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data");
        Assert.True(data.TryGetProperty("conversations", out var convs));
        Assert.True(convs.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task GET_messaging_properties_propertyId_conversations_conversationId_returns_ok_and_messages()
    {
        var response = await _client.GetAsync($"/messaging/properties/{PropertyId}/conversations/{ConversationId}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Real API shape: { meta, data: { conversation: { ... }, ok }, errors, warnings }
        Assert.True(json.TryGetProperty("meta", out _));
        var data = json.GetProperty("data");
        Assert.True(data.GetProperty("ok").GetBoolean());
        var conv = data.GetProperty("conversation");
        Assert.Equal(ConversationId, conv.GetProperty("conversation_id").GetString());
        Assert.True(conv.TryGetProperty("messages", out _));
        Assert.True(conv.TryGetProperty("participants", out _));
        Assert.True(conv.TryGetProperty("access", out _));
        Assert.True(conv.TryGetProperty("tags", out _));
    }

    [Fact]
    public async Task POST_messaging_properties_propertyId_conversations_conversationId_returns_ok_and_message_id()
    {
        var body = JsonSerializer.Serialize(new { message = new { content = "Test message from test", attachment_ids = Array.Empty<string>() } });
        var response = await _client.PostAsync(
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Real API shape: { meta, data: { message_id, ok, guest_has_account }, errors, warnings }
        Assert.True(json.TryGetProperty("meta", out _));
        var data = json.GetProperty("data");
        Assert.True(data.TryGetProperty("message_id", out var msgId));
        Assert.False(string.IsNullOrEmpty(msgId.GetString()));
        Assert.True(data.GetProperty("ok").GetBoolean());
        Assert.True(data.GetProperty("guest_has_account").GetBoolean());
    }

    [Fact]
    public async Task GET_messaging_messages_search_returns_ok_and_job_id()
    {
        var response = await _client.GetAsync("/messaging/messages/search?after=2020-01-01T00:00:00Z&before=2030-01-01T00:00:00Z");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data");
        Assert.False(string.IsNullOrEmpty(data.GetProperty("job_id").GetString()));
        Assert.True(data.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task GET_messaging_messages_search_result_jobId_returns_ok_after_creating_job()
    {
        var searchResponse = await _client.GetAsync("/messaging/messages/search?property_id=" + PropertyId);
        searchResponse.EnsureSuccessStatusCode();
        var searchJson = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jobId = searchJson.GetProperty("data").GetProperty("job_id").GetString();
        Assert.NotNull(jobId);

        var resultResponse = await _client.GetAsync($"/messaging/messages/search/result/{jobId}");
        resultResponse.EnsureSuccessStatusCode();
        var resultJson = await resultResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(resultJson.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("messages", out _));
    }

    [Fact]
    public async Task GET_root_returns_html_index_page()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Booking", html);
        Assert.Contains("Conversation", html);
    }

    [Fact]
    public async Task GET_conversation_page_returns_html()
    {
        var response = await _client.GetAsync($"/Conversation/{PropertyId}/{ConversationId}");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(ConversationId, html);
        Assert.Contains("btn-warning", html); // Send button is present
    }

    [Fact]
    public async Task GET_new_conversation_page_returns_html()
    {
        var response = await _client.GetAsync($"/NewConversation/{PropertyId}");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("New conversation", html);
        Assert.Contains("Conversation reference", html);
    }

    [Fact]
    public async Task GET_messaging_properties_unknown_returns_404()
    {
        var response = await _client.GetAsync("/messaging/properties/unknown-property-999/conversations");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_messaging_messages_search_result_unknown_job_returns_404()
    {
        var response = await _client.GetAsync("/messaging/messages/search/result/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_conversation_tag_no_reply_needed_sets_and_reflects_in_get()
    {
        // PUT sets the tag (no body required)
        var putResponse = await _client.PutAsync(
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}/tags/no_reply_needed",
            new StringContent("", System.Text.Encoding.UTF8, "application/json"));
        putResponse.EnsureSuccessStatusCode();
        var putJson = await putResponse.Content.ReadFromJsonAsync<JsonElement>();
        var putData = putJson.GetProperty("data");
        Assert.True(putData.GetProperty("ok").GetBoolean());
        Assert.Equal("no_reply_needed", putData.GetProperty("tag").GetString());
        Assert.True(putData.GetProperty("is_set").GetBoolean());

        // Verify reflected in GET conversation detail
        var getJson = await (await _client.GetAsync(
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(getJson.GetProperty("data").GetProperty("conversation")
            .GetProperty("tags").GetProperty("no_reply_needed").GetProperty("set").GetBoolean());

        // DELETE removes the tag
        var deleteResponse = await _client.DeleteAsync(
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}/tags/no_reply_needed");
        deleteResponse.EnsureSuccessStatusCode();
        var deleteJson = await deleteResponse.Content.ReadFromJsonAsync<JsonElement>();
        var deleteData = deleteJson.GetProperty("data");
        Assert.True(deleteData.GetProperty("ok").GetBoolean());
        Assert.Equal("no_reply_needed", deleteData.GetProperty("tag").GetString());
        Assert.False(deleteData.GetProperty("is_set").GetBoolean());
    }

    [Fact]
    public async Task PUT_DELETE_message_read_tag_sets_and_unsets()
    {
        // Post a message first to get a message_id
        var sendBody = JsonSerializer.Serialize(new { message = new { content = "Tag read test" } });
        var sendJson = await (await _client.PostAsync(
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}",
            new StringContent(sendBody, System.Text.Encoding.UTF8, "application/json")))
            .Content.ReadFromJsonAsync<JsonElement>();
        var messageId = sendJson.GetProperty("data").GetProperty("message_id").GetString();
        Assert.NotNull(messageId);

        var participantId = "893af63e-b32a-5751-af59-b657dc5c9602";

        // PUT tags the messages as read
        var putBody = JsonSerializer.Serialize(new { message_ids = new[] { messageId }, participant_id = participantId });
        var putResponse = await _client.PutAsync(
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}/tags/message_read",
            new StringContent(putBody, System.Text.Encoding.UTF8, "application/json"));
        putResponse.EnsureSuccessStatusCode();
        var putJson = await putResponse.Content.ReadFromJsonAsync<JsonElement>();
        var putData = putJson.GetProperty("data");
        Assert.True(putData.GetProperty("ok").GetBoolean());
        Assert.Equal("read", putData.GetProperty("tag").GetString());
        Assert.True(putData.GetProperty("is_set").GetBoolean());

        // Verify reflected in GET conversation detail
        var getJson = await (await _client.GetAsync(
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var messages = getJson.GetProperty("data").GetProperty("conversation").GetProperty("messages");
        var match = messages.EnumerateArray().FirstOrDefault(m => m.GetProperty("message_id").GetString() == messageId);
        Assert.True(match.GetProperty("tags").GetProperty("read").GetProperty("set").GetBoolean());

        // DELETE unmarks the messages as read
        var deleteBody = JsonSerializer.Serialize(new { message_ids = new[] { messageId }, participant_id = participantId });
        var deleteResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/messaging/properties/{PropertyId}/conversations/{ConversationId}/tags/message_read")
        {
            Content = new StringContent(deleteBody, System.Text.Encoding.UTF8, "application/json")
        });
        deleteResponse.EnsureSuccessStatusCode();
        var deleteJson = await deleteResponse.Content.ReadFromJsonAsync<JsonElement>();
        var deleteData = deleteJson.GetProperty("data");
        Assert.True(deleteData.GetProperty("ok").GetBoolean());
        Assert.Equal("read", deleteData.GetProperty("tag").GetString());
        // Real API returns is_set:true for DELETE message_read (means "operation succeeded", not current state)
        Assert.True(deleteData.GetProperty("is_set").GetBoolean());
    }
}
