using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Configurations;

public class TextResponseFormat
{
    public string MimeType { get; set; } = "application/json";
    public JsonNode? Schema { get; set; }
}
