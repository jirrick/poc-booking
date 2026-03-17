using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new { service = "PocBooking.BookingSimulator", role = "Simulates Booking.com CNS" }));

// Returns a sample MESSAGING_API_NEW_MESSAGE payload for manual use or inspection.
app.MapGet("/api/simulate/sample", () =>
{
    var payload = BuildSampleNotification(Guid.NewGuid(), Guid.NewGuid().ToString());
    return Results.Json(payload, new JsonSerializerOptions { WriteIndented = true });
});

// Simulates CNS: sends a notification to the POC webhook. Optional body can override message_id or content.
app.MapPost("/api/simulate/deliver", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    CancellationToken ct) =>
{
    var baseUrl = config["BookingSimulator:PocWebhookBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5154";
    var webhookUrl = $"{baseUrl}/api/webhooks/booking/cns";
    var bearerToken = config["BookingSimulator:PocBearerToken"]; // optional

    object payload;
    try
    {
        var body = await context.Request.ReadFromJsonAsync<SimulateDeliverRequest>(ct);
        payload = BuildSampleNotification(
            body?.NotificationUuid ?? Guid.NewGuid(),
            body?.MessageId ?? Guid.NewGuid().ToString(),
            body?.Content);
    }
    catch
    {
        payload = BuildSampleNotification(Guid.NewGuid(), Guid.NewGuid().ToString(), null);
    }

    using var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json")
    };
    if (!string.IsNullOrEmpty(bearerToken))
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

    var response = await client.SendAsync(request, ct);
    var responseBody = await response.Content.ReadAsStringAsync(ct);
    return Results.Json(new
    {
        pocStatusCode = (int)response.StatusCode,
        pocResponse = responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody,
        webhookUrl
    }, statusCode: 200);
});

app.Run();

static object BuildSampleNotification(Guid notificationUuid, string messageId, string? content = null)
{
    return new
    {
        metadata = new
        {
            uuid = notificationUuid.ToString(),
            type = "MESSAGING_API_NEW_MESSAGE",
            payloadVersion = "1.0"
        },
        payload = new
        {
            message_id = messageId,
            message_type = "free_text",
            timestamp = DateTime.UtcNow.ToString("O"),
            reply_to = (string?)null,
            content = content ?? "Simulated message from BookingSimulator",
            attachment_ids = Array.Empty<string>(),
            sender = new
            {
                participant_id = "9f6be5fd-b3a8-5691-9cf9-9ab6c6217327",
                metadata = new { name = "Test Property", participant_type = "hotel" }
            },
            conversation = new
            {
                property_id = "1383087",
                conversation_id = "f3a9c29d-480d-5f5b-a6c0-65451e335353",
                conversation_reference = "3812391309",
                conversation_type = "reservation"
            }
        }
    };
}

internal sealed class SimulateDeliverRequest
{
    public Guid? NotificationUuid { get; set; }
    public string? MessageId { get; set; }
    public string? Content { get; set; }
}
