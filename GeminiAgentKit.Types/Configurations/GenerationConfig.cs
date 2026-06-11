using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Configurations;

public class GenerationConfig
{
    public string? ResponseMimeType { get; set; }
    public JsonNode? ResponseSchema { get; set; }
}
