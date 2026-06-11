using GeminiAgentKit.Types.Configurations;
using GeminiAgentKit.Types.Contents;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Types.Requests;

public class GenerateContentRequest
{
    public List<Content> Contents { get; set; } = new();
    public Content? SystemInstruction { get; set; }
    public GenerationConfig? GenerationConfig { get; set; }
    public List<Tool> Tools { get; set; } = new();
}
