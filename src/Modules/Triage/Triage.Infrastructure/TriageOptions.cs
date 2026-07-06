namespace Triage.Infrastructure;

public sealed class TriageOptions
{
    public const string SectionName = "Triage";

    public PresidioOptions Presidio { get; set; } = new();
    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();
    public AnthropicOptions Anthropic { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
    public CacheOptions Cache { get; set; } = new();

    public sealed class CacheOptions
    {
        /// <summary>How long a triage result for a given masked ticket text stays cached.</summary>
        public int TtlHours { get; set; } = 24;
    }

    public sealed class PresidioOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:5002";
    }

    public sealed class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "llama3.1";
        /// <summary>Local inference gets a longer timeout and no rate limiting, unlike cloud providers.</summary>
        public int TimeoutSeconds { get; set; } = 60;
    }

    public sealed class OpenAiOptions
    {
        public string BaseUrl { get; set; } = "https://api.openai.com";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public int TimeoutSeconds { get; set; } = 15;
    }

    public sealed class AnthropicOptions
    {
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "claude-3-5-sonnet-latest";
        public int TimeoutSeconds { get; set; } = 15;
    }

    public sealed class GeminiOptions
    {
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-1.5-flash";
        public int TimeoutSeconds { get; set; } = 15;
    }
}
