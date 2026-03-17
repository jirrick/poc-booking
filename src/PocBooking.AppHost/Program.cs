var builder = DistributedApplication.CreateBuilder(args);

// Ports 5154 (Api) and 5160 (Simulator) come from each project's launchSettings.json applicationUrl.
// Aspire reads those and creates the "http" proxy endpoints automatically.
builder.AddProject("poc-api", "../PocBooking.Api/PocBooking.Api.csproj")
    .WithEnvironment("Booking__ApiBaseUrl", "http://localhost:5160");

builder.AddProject("simulator", "../PocBooking.BookingSimulator/PocBooking.BookingSimulator.csproj")
    .WithEnvironment("BookingSimulator__PocWebhookBaseUrl", "http://localhost:5154");

await builder.Build().RunAsync();
