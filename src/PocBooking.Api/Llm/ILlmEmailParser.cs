namespace PocBooking.Api.Llm;

public interface ILlmEmailParser
{
    /// <summary>Returns the model IDs available on the configured LLM server.</summary>
    Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends <paramref name="emailContent"/> to the LLM using the active model and system prompt.
    /// Returns the parsed text, or <c>null</c> if no model is selected.
    /// </summary>
    Task<string?> ParseAsync(string emailContent, CancellationToken ct = default);
}

