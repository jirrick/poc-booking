using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PocBooking.BookingSimulator.Data;
using PocBooking.BookingSimulator.Endpoints;
using PocBooking.BookingSimulator.Models;
using PocBooking.BookingSimulator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<SimulatorDbContext>(options => options.UseSqlite(conn));
builder.Services.AddScoped<SimulatorDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SimulatorDbContext>>().CreateDbContext());
builder.Services.Configure<PocWebhookJwtOptions>(builder.Configuration.GetSection(PocWebhookJwtOptions.SectionName));
builder.Services.AddSingleton<IPocWebhookJwtFactory, PocWebhookJwtFactory>();
builder.Services.AddScoped<IPocWebhookSender, PocWebhookSender>();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

var app = builder.Build();

await using (var db = app.Services.GetRequiredService<IDbContextFactory<SimulatorDbContext>>().CreateDbContext())
{
    await db.Database.MigrateAsync();
    await SimulatorDbSeed.SeedAsync(db);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapMessagingEndpoints();

app.MapGet("/api", () => Results.Ok(new { service = "PocBooking.BookingSimulator", role = "Simulates Booking.com CNS" }));

// Returns a sample MESSAGING_API_NEW_MESSAGE payload for manual use or inspection.
app.MapGet("/api/simulate/sample", () =>
{
    var payload = BuildSamplePayload(Guid.NewGuid(), Guid.NewGuid().ToString(), null);
    return Results.Json(payload, new JsonSerializerOptions { WriteIndented = true });
});

// Simulates CNS: sends a notification to the POC webhook using the shared webhook sender. Optional body can override message_id or content.
app.MapPost("/api/simulate/deliver", async (
    HttpContext context,
    IPocWebhookSender webhookSender,
    IConfiguration config,
    CancellationToken ct) =>
{
    var baseUrl = config["BookingSimulator:PocWebhookBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5154";
    var webhookUrl = $"{baseUrl}/api/webhooks/booking/cns";

    CnsWebhookPayload payload;
    try
    {
        var body = await context.Request.ReadFromJsonAsync<SimulateDeliverRequest>(ct);
        payload = BuildSamplePayload(
            body?.NotificationUuid ?? Guid.NewGuid(),
            body?.MessageId ?? Guid.NewGuid().ToString(),
            body?.Content);
    }
    catch
    {
        payload = BuildSamplePayload(Guid.NewGuid(), Guid.NewGuid().ToString(), null);
    }

    await webhookSender.SendPayloadAsync(payload, ct);

    return Results.Json(new
    {
        ok = true,
        webhookUrl,
        message = "Payload sent via shared webhook sender"
    }, statusCode: 200);
});

app.Run();

static CnsWebhookPayload BuildSamplePayload(Guid notificationUuid, string messageId, string? content)
{
    return new CnsWebhookPayload
    {
        Metadata = new CnsMetadata
        {
            Uuid = notificationUuid.ToString(),
            Type = "MESSAGING_API_NEW_MESSAGE",
            PayloadVersion = "1.0"
        },
        Payload = new CnsMessagePayload
        {
            MessageId = messageId,
            MessageType = "free_text",
            Timestamp = DateTime.UtcNow.ToString("O"),
            ReplyTo = null,
            Content = content ?? "Simulated message from BookingSimulator",
            AttachmentIds = [],
            Sender = new CnsSender
            {
                ParticipantId = "9f6be5fd-b3a8-5691-9cf9-9ab6c6217327",
                Metadata = new CnsSenderMetadata { Name = "Test Property", ParticipantType = "hotel" }
            },
            Conversation = new CnsConversation
            {
                PropertyId = "1383087",
                ConversationId = "f3a9c29d-480d-5f5b-a6c0-65451e335353",
                ConversationReference = "3812391309",
                ConversationType = "reservation"
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
