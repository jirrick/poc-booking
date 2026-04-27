using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PocBooking.BookingSimulator.Services;

namespace PocBooking.BookingSimulator.Pages;

public class ApiLogModel : PageModel
{
    private readonly ApiRequestLogStore _store;

    public ApiLogModel(ApiRequestLogStore store) => _store = store;

    public IReadOnlyList<ApiRequestLogEntry> Entries { get; private set; } = [];

    public void OnGet() => Entries = _store.GetAll();

    public IActionResult OnPostClear()
    {
        _store.Clear();
        return RedirectToPage();
    }
}

