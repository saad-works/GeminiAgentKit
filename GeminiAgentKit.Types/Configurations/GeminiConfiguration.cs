namespace GeminiAgentKit.Types.Config;

/// <summary>
/// Holds the API credentials and endpoint configuration required to communicate with the Gemini API.
/// Populated from the <c>GeminiConfiguration</c> section of <c>appsettings.json</c>.
/// </summary>
public class GeminiConfiguration
{
    /// <summary>
    /// Base URL template for the Gemini <c>generateContent</c> endpoint.
    /// The <c>{{model}}</c> placeholder is replaced at runtime with the active model identifier.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Google AI Studio API key used to authenticate requests.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The active model identifier, defaulting to <see cref="GeminiAgentKit.Types.System.Model.Gemma431B"/> when loaded from config.</summary>
    public string Model { get; set; } = string.Empty;
}
