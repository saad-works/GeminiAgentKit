using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Tools;

public class FunctionResponse
{
    public required string Name { get; set; }
    public JsonNode? Response { get; set; }
}
