using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PocBooking.Api.Llm;

public class LlmEmailParser(HttpClient http, LlmSettingsStore settings) : ILlmEmailParser
{
    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetFromJsonAsync<ModelsResponse>("/v1/models", ct);
            return response?.Data?.Select(m => m.Id).Where(id => id != null).Cast<string>().ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<string?> ParseAsync(string emailContent, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(settings.SelectedModel))
            return null;

        var request = new
        {
            model = settings.SelectedModel,
            messages = new[]
            {
                new { role = "system", content = settings.SystemPrompt },
                new { role = "user",   content = emailContent }
            },
            temperature = 0.1,
            max_tokens = 1024,
            stream = false
        };

        using var response = await http.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(ct);
        return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
    }

    // ── JSON shapes ───────────────────────────────────────────────────────────

    private sealed class ModelsResponse
    {
        [JsonPropertyName("data")] public List<ModelEntry>? Data { get; set; }
    }

    private sealed class ModelEntry
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")] public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}

