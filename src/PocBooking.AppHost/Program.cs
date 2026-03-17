var builder = DistributedApplication.CreateBuilder(args);

// POC API (webhook receiver, property UI, inbox). Fixed port so simulator can call webhook.
var pocApi = builder.AddProject("poc-api", "../PocBooking.Api/PocBooking.Api.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5154")
    .WithEnvironment("Booking__ApiBaseUrl", "http://localhost:5160");

// Booking simulator (CNS + messaging API). Fixed port so POC UI can call it.
var simulator = builder.AddProject("simulator", "../PocBooking.BookingSimulator/PocBooking.BookingSimulator.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5160")
    .WithEnvironment("BookingSimulator__PocWebhookBaseUrl", "http://localhost:5154");

await builder.Build().RunAsync();
