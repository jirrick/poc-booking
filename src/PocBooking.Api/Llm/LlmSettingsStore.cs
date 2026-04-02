namespace PocBooking.Api.Llm;

/// <summary>
/// Singleton that holds the operator-chosen LLM model and system prompt for the current session.
/// Reset on app restart (POC only).
/// </summary>
public class LlmSettingsStore
{
    public string? SelectedModel { get; set; }
    public string SystemPrompt { get; set; } = LlmDefaults.SystemPrompt;
}

