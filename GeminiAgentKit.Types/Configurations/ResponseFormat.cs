namespace GeminiAgentKit.Types.Configurations;

/// <summary>Wraps a <see cref="TextResponseFormat"/> for use in generation configuration.</summary>
public class ResponseFormat
{
    /// <summary>The text response format settings, including MIME type and optional JSON Schema.</summary>
    public TextResponseFormat? Text { get; set; }
}
