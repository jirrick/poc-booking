using PocBooking.Api.Llm;

namespace PocBooking.Api.Endpoints;

public static class LlmEndpoints
{
    public static void MapLlmEndpoints(this IEndpointRouteBuilder routes)
    {
        var g = routes.MapGroup("/api/llm");

        // POST /api/llm/parse  { content }  → { parsed }
        g.MapPost("/parse", async (ParseRequest req, ILlmEmailParser parser, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Results.BadRequest(new { error = "content is required" });

            string? parsed;
            try
            {
                parsed = await parser.ParseAsync(req.Content, ct);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 502);
            }

            if (parsed is null)
                return Results.BadRequest(new { error = "No model selected. Configure one on the home page." });

            return Results.Ok(new { parsed });
        });
    }

    private sealed record ParseRequest(string Content);
}

