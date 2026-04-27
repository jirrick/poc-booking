using System.Diagnostics;
using PocBooking.BookingSimulator.Services;

namespace PocBooking.BookingSimulator;

public sealed class ApiRequestLoggingMiddleware(RequestDelegate next, ApiRequestLogStore store)
{
    private static readonly HashSet<string> IncludedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Accept", "Content-Type", "Accept-Version", "User-Agent", "Host"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var isApiPath =
            path.StartsWith("/messaging", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/token-based-authentication", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "/api", StringComparison.OrdinalIgnoreCase);

        // For non-API paths we still want to capture errors — run pipeline first, then decide.
        if (!isApiPath)
        {
            var sw0 = Stopwatch.StartNew();
            await next(context);
            sw0.Stop();

            if (context.Response.StatusCode >= 400)
            {
                store.Add(new ApiRequestLogEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Method = context.Request.Method,
                    Path = path,
                    QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                    StatusCode = context.Response.StatusCode,
                    ElapsedMs = sw0.ElapsedMilliseconds,
                    RequestHeaders = new Dictionary<string, string>()
                });
            }
            return;
        }

        context.Request.EnableBuffering();

        string? requestBody = null;
        if (context.Request.ContentLength is > 0 and <= 8192)
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        var headers = context.Request.Headers
            .Where(h => IncludedHeaders.Contains(h.Key))
            .ToDictionary(
                h => h.Key,
                h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                    ? RedactBearer(h.Value.ToString())
                    : h.Value.ToString());

        var sw = Stopwatch.StartNew();
        await next(context);
        sw.Stop();

        store.Add(new ApiRequestLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Method = context.Request.Method,
            Path = path,
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            StatusCode = context.Response.StatusCode,
            ElapsedMs = sw.ElapsedMilliseconds,
            RequestBody = requestBody,
            RequestHeaders = headers
        });
    }

    private static string RedactBearer(string value)
    {
        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && value.Length > 20)
            return value[..14] + "...[redacted]";
        return value;
    }
}




