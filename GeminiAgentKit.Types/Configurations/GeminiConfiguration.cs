using GeminiModel = GeminiAgentKit.Types.System.Model;

namespace GeminiAgentKit.Types.Config;

/// <summary>
/// Holds the API credentials and endpoint configuration required to communicate with the Gemini API.
/// Only <see cref="ApiKey"/> is required; everything else has a sensible default.
/// </summary>
public class GeminiConfiguration
{
    /// <summary>The default <c>generateContent</c> endpoint template.</summary>
    public const string DefaultBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/{{model}}:generateContent";

    /// <summary>
    /// Base URL template for the Gemini <c>generateContent</c> endpoint.
    /// The <c>{{model}}</c> placeholder is replaced at runtime with the active model identifier.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>Google AI Studio API key used to authenticate requests.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The model used when a call passes a null or empty model.
    /// Defaults to <see cref="GeminiModel.Gemini25Flash"/>.
    /// </summary>
    public string Model { get; set; } = GeminiModel.Gemini25Flash;
}
