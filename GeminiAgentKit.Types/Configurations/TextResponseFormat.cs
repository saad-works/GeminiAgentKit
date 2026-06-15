using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Configurations;

/// <summary>Specifies the MIME type and optional JSON Schema for a text response.</summary>
public class TextResponseFormat
{
    /// <summary>MIME type of the response, e.g. <c>application/json</c>. Defaults to <c>application/json</c>.</summary>
    public string MimeType { get; set; } = "application/json";

    /// <summary>Optional JSON Schema the model's response must conform to.</summary>
    public JsonNode? Schema { get; set; }
}
