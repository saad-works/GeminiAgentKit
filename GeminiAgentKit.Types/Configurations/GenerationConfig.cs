using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Configurations;

/// <summary>Controls the format and schema of the model's generated response.</summary>
public class GenerationConfig
{
    /// <summary>
    /// MIME type of the expected response, e.g. <c>application/json</c>.
    /// Set automatically when using the typed <c>GenerateContentAsync&lt;T&gt;</c> overload.
    /// </summary>
    public string? ResponseMimeType { get; set; }

    /// <summary>
    /// JSON Schema constraining the structure of the model's response.
    /// Set automatically from the type argument when using <c>GenerateContentAsync&lt;T&gt;</c>.
    /// </summary>
    public JsonNode? ResponseSchema { get; set; }
}
