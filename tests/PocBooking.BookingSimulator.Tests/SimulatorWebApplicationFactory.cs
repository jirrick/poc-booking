using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PocBooking.BookingSimulator.Tests;

public sealed class SimulatorWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string TestDbPath = Path.Combine(Path.GetTempPath(), $"simulator-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={TestDbPath}",
                ["BookingSimulator:SendWebhookOnNewMessage"] = "false",
                ["BookingSimulator:PocWebhookBaseUrl"] = ""
            });
        });
    }
}
