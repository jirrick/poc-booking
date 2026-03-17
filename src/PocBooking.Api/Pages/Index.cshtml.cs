using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PocBooking.Api.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _config;

    public IndexModel(IConfiguration config) => _config = config;

    /// <summary>Default property ID for simulator (seeded). Use when Booking API is simulator.</summary>
    public string DefaultPropertyId { get; set; } = "1383087";

    public void OnGet()
    {
    }
}
