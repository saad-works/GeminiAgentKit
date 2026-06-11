using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Tools;

public class FunctionCall
{
    public required string Name { get; set; }
    public JsonNode? Args { get; set; }
}
