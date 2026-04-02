var builder = DistributedApplication.CreateBuilder(args);

var isLive = builder.Environment.EnvironmentName == "BookingLive";

// Ports 5154 (Api) and 5160 (Simulator) come from each project's launchSettings.json applicationUrl.
// Aspire reads those and creates the "http" proxy endpoints automatically.
var api = builder.AddProject("poc-api", "../PocBooking.Api/PocBooking.Api.csproj")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", isLive ? "BookingLive" : "Simulator");

if (!isLive)
{
    // In the default Simulator profile the API talks to the local simulator.
    api.WithEnvironment("Booking__ApiBaseUrl", "http://localhost:5160");

    builder.AddProject("simulator", "../PocBooking.BookingSimulator/PocBooking.BookingSimulator.csproj")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Simulator")
        .WithEnvironment("BookingSimulator__PocWebhookBaseUrl", "http://localhost:5154");
}
// In BookingLive the API uses appsettings.BookingLive.json (real Booking.com URLs)
// and its credentials come from user secrets (UserSecretsId: poc-booking-api).
// In Simulator mode appsettings.Simulator.json is used with hard-coded local values.

await builder.Build().RunAsync();


