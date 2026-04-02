namespace PocBooking.Api.Llm;

public class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>Base URL of the OpenAI-compatible LLM server (e.g. http://localhost:11434 for Ollama).</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";
}

