using MediatR;
using Microsoft.EntityFrameworkCore;
using PocBooking.Api.Auth;
using PocBooking.Api.BookingApi;
using PocBooking.Api.Data;
using PocBooking.Api.Endpoints;
using PocBooking.Api.Enrichment;
using PocBooking.Api.Mapping;
using PocBooking.Api.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CnsJwtOptions>(builder.Configuration.GetSection(CnsJwtOptions.SectionName));
builder.Services.AddSingleton<ICnsSignatureValidator, CnsJwtSignatureValidator>();

builder.Services.Configure<BookingApiOptions>(builder.Configuration.GetSection(BookingApiOptions.SectionName));
builder.Services.AddHttpClient<IBookingApiClient, BookingApiClient>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IEnrichCnsMessage, EnrichCnsMessageService>();
builder.Services.AddScoped<IConversationMappingService, ConversationMappingService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ProcessCnsNotificationHandler>());

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Apply any pending EF Core migrations (creates the DB on first run, upgrades on schema changes).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/api", () => Results.Ok(new { service = "PocBooking.Api", status = "running" }));

app.MapGet("/api/health", async (AppDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.StatusCode(503);
});

app.MapBookingCnsWebhook();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Index"));

app.Run();
